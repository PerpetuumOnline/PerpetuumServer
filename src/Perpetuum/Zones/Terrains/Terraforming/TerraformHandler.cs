using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Terrains.Terraforming.Operations;

namespace Perpetuum.Zones.Terrains.Terraforming
{
    public enum TerraformType
    {
        Undefined,
        Blur,
        Level,
        Simple,

    }

    public struct AffectedTile
    {
        public readonly int x;
        public readonly int y;
        public readonly TerraformType Type;

        public AffectedTile(int x, int y, TerraformType Type)
        {
            this.x = x;
            this.y = y;
            this.Type = Type;

        }
    }


    public class TerraformHandler : Process
    {
        private readonly ConcurrentBag<AffectedTile> _affectedTiles = new ConcurrentBag<AffectedTile>();

        private readonly IZone _zone;
        private readonly ConcurrentQueue<ITerraformingOperation> _terraformingOperations = new ConcurrentQueue<ITerraformingOperation>();

        private Area _degradeArea;

        public delegate TerraformHandler Factory(IZone zone);

        public TerraformHandler(IZone zone)
        {
            _zone = zone;
            _degradeArea = Area.FromRectangle(0, 0,zone.Size.Width,zone.Size.Height);
            Degrade = true;
        }

        public bool Degrade { get; set; }

        //csak module
        public void EnqueueTerraformingOperation(ITerraformingOperation terraformingOperation)
        {
            terraformingOperation.Prepare(_zone);
            _terraformingOperations.Enqueue(terraformingOperation);
        }

        private void DequeueTerraformingOperations()
        {
            if (_terraformingOperations.Count == 0)
                return;

            using (new TerrainUpdateMonitor(_zone))
            {
                var selector = new FeedbackTypeSelector();

                ITerraformingOperation terraformingOperation;
                while (_terraformingOperations.TryDequeue(out terraformingOperation))
                {
                    terraformingOperation.DoTerraform(_zone);
                    terraformingOperation.AcceptVisitor(selector);

                    if (selector.Type == TerraformType.Undefined)
                        continue;

                    var preparedArea = new Area(terraformingOperation.TerraformArea.X1 - 1,
                                                terraformingOperation.TerraformArea.Y1 - 1,
                                                terraformingOperation.TerraformArea.X2,
                                                terraformingOperation.TerraformArea.Y2).Clamp(_zone.Size);

                    foreach (var position in preparedArea.GetPositions())
                    {
                        _affectedTiles.Add(new AffectedTile(position.intX, position.intY, selector.Type));
                    }
                }
            }
        }

        private class FeedbackTypeSelector : TerraformingOperationVisitor
        {
            public TerraformType Type { get; private set; }

            public override void VisitTerraformingOperation(ITerraformingOperation operation)
            {
                Type = TerraformType.Undefined;
            }

            public override void VisitBlurTerraformingOperation(BlurTerraformingOperation operation)
            {
                Type = TerraformType.Blur;
            }

            public override void VisitLevelTerraformingOperation(LevelTerraformingOperation operation)
            {
                Type = TerraformType.Level;
            }

            public override void VisitSingleTileTerraformingOperation(SimpleTileTerraformingOperation operation)
            {
                Type = TerraformType.Simple;
            }
        }

        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(10));
        private readonly IntervalTimer _degradeTimer = new IntervalTimer(TimeSpan.FromSeconds(5));

        public override void Update(TimeSpan time)
        {
            _degradeTimer.Update(time);
            if (_degradeTimer.Passed)
            {
                _degradeTimer.Reset();
                var o = new TerrainDegradeOperation(_degradeArea);
                EnqueueTerraformingOperation(o);
            }

            DequeueTerraformingOperations();

            _timer.Update(time);
            if (_timer.Passed)
            {
                _timer.Reset();
                ProcessAffectedPositions();
            }
        }

        private void ProcessAffectedPositions()
        {
            if (!_affectedTiles.Any())
                return;

            SendAffectedPositions();
        }

        private void SendAffectedPositions()
        {
            var affectedTiles = new Dictionary<Point,TerraformType>();

            AffectedTile tile;
            while (_affectedTiles.TryTake(out tile))
            {
                affectedTiles[new Point(tile.x,tile.y)] = tile.Type;
            }

            foreach (var pair in affectedTiles)
            {
                var opType = pair.Value;
                var position = pair.Key;

                var builder = Beam.NewBuilder().WithDuration(5000).WithState(BeamState.AlignToTerrain).WithPosition(position.ToPosition().Center);

                switch (opType)
                {
                    case TerraformType.Blur:
                        builder.WithType(BeamType.red_10sec);
                        break;
                    case TerraformType.Level:
                        builder.WithType(BeamType.green_10sec);
                        break;
                    case TerraformType.Simple:
                        builder.WithType(BeamType.blue_10sec);
                        break;
                }

                _zone.CreateBeam(builder);
            }
        }
    }
}