using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Log;
using Perpetuum.Threading.Process;
using Perpetuum.Zones.PBS.HighwayNode;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones.PBS
{
    public class HighwaySegmentInfo
    {
        public Position StatPosition;
        public Position EndPosition;
        public int Radius;

        public bool IsPointWithinRange(double x, double y)
        {
            double xa = StatPosition.X;
            double ya = StatPosition.Y;
            double xb = EndPosition.X;
            double yb = EndPosition.Y;
            var distance = MathHelper.DistanceFromLineSegment(xa, ya, xb, yb, x, y);

            return distance <= Radius;
        }

        public Area BoundingArea()
        {
            var xMin = (int)Math.Floor(Math.Min(StatPosition.X, EndPosition.X) - Radius);
            var yMin = (int)Math.Floor(Math.Min(StatPosition.Y, EndPosition.Y) - Radius);
            var xMax = (int)Math.Ceiling(Math.Max(StatPosition.X, EndPosition.X) + Radius);
            var yMax = (int)Math.Ceiling(Math.Max(StatPosition.Y, EndPosition.Y) + Radius);

            var a = new Area(xMin, yMin, xMax, yMax);

            return a;
        }

    }

    //draws on the terrain based on the highway nodes
    public class PBSHighwayHandler : Process
    {
        public delegate PBSHighwayHandler Factory(IZone zone);

        public const int DRAW_INTERVAL = 5000;

        private readonly IZone _zone;
        private readonly ConcurrentQueue<Area> _areaQueue = new ConcurrentQueue<Area>();

        public PBSHighwayHandler(IZone zone)
        {
            _zone = zone;
        }

        public override void Update(TimeSpan time)
        {
            ProcessStuff();
        }

        public override void Start()
        {
            Init();
            base.Start();
        }

        /// <summary>
        /// Clear pbs highway
        /// </summary>
        private void Init()
        {
            for (var i = 0; i < _zone.Terrain.Controls.RawData.Length; i++)
            {
                _zone.Terrain.Controls.RawData[i].PBSHighway = false;
            }
        }

        private void ProcessStuff()
        {
            if (_areaQueue.Count == 0) 
                return;

            var liveSegments = GetLiveHighwaySegments();
            ProcessEnquedAreas(liveSegments);
        }

        private List<HighwaySegmentInfo> GetLiveHighwaySegments()
        {
            var l = new List<HighwaySegmentInfo>();

            var highwayNodes = _zone.Units.OfType<PBSHighwayNode>().ToList();

            foreach (var pbsHighwayNode in highwayNodes)
            {
                l.AddRange(pbsHighwayNode.GetOutgoingLiveSegments());
            }

            Logger.DebugInfo($"collected {l.Count} live segments");
            return l;
        }

        private void ProcessEnquedAreas(List<HighwaySegmentInfo> segments)
        {
            var areas = _areaQueue.TakeAll().ToList();

            Logger.DebugInfo($"processing {areas.Count} highway areas");

            areas = areas.Distinct().ToList();

            Logger.DebugInfo($"processing {areas.Count} unique areas");


            using (new TerrainUpdateMonitor(_zone))
            {
                foreach (var eArea in areas)
                {
                    //clamp to current zone
                    var zArea = eArea.Clamp(_zone.Configuration.Size);

                    //get the original data
                    var data = _zone.Terrain.Controls.GetArea(zArea);

                    //clear highway bit
                    for (var i = 0; i < data.Length; i++)
                    {
                        var tc = data[i];
                        tc.PBSHighway = false;
                        tc.ConcreteA = false;
                        tc.ConcreteB = false;
                        data[i] = tc;
                    }

                    //set highway if in any segment
                    for (var j = zArea.Y1; j <= zArea.Y2; j++)
                    {
                        for (var i = zArea.X1; i <= zArea.X2; i++)
                        {
                            if (segments.Any(s => s.IsPointWithinRange(i + 0.5, j + 0.5)))
                            {
                                var xo = i - zArea.X1;
                                var yo = j - zArea.Y1;

                                var offset = xo + yo*zArea.Width;

                                var tc = data[offset];
                                
                                //do highway
                                tc.PBSHighway = true;
                                
                                //draw concrete
                                if (FastRandom.NextInt() % 2 == 1)
                                {
                                    tc.ConcreteA = true;
                                }
                                else
                                {
                                    tc.ConcreteB = true;
                                }

                                data[offset] = tc;
                            }
                        }
                    }

                    _zone.Terrain.Controls.SetArea(zArea, data);
                }
            }
        }

        public void SubmitMore(IEnumerable<Area> areas)
        {
            _areaQueue.EnqueueMany(areas);
        }
    }
}
