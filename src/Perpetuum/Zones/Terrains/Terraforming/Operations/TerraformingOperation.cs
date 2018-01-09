using System;
using System.Linq;
using Perpetuum.Units;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones.Terrains.Terraforming.Operations
{

    public interface ITerraformingOperation
    {
        void Prepare(IZone zone);
        void DoTerraform(IZone zone);
        Area TerraformArea { get; }
        void AcceptVisitor(TerraformingOperationVisitor visitor);
    }

    public class TerraformingOperationVisitor
    {
        public virtual void VisitTerraformingOperation(ITerraformingOperation operation)
        {
        }


        public virtual void VisitTerraformingOperation(TerraformingOperation operation)
        {
            VisitTerraformingOperation((ITerraformingOperation)operation);
        }

        public virtual void VisitBlurTerraformingOperation(BlurTerraformingOperation operation)
        {
            VisitTerraformingOperation(operation);
        }

        public virtual void VisitLevelTerraformingOperation(LevelTerraformingOperation operation)
        {
            VisitTerraformingOperation(operation);
        }
        
        public virtual void VisitSingleTileTerraformingOperation(SimpleTileTerraformingOperation operation)
        {
            VisitTerraformingOperation(operation);
        }
    }


    public abstract class TerraformingOperation : ITerraformingOperation
    {
        private const int SLOPE_THRESHOLD = 19;
        private readonly int _plantDamage;
        private ushort[] _buffer;
        private Area _bufferArea;
       
        protected abstract int ProduceDirection(IZone zone, int x, int y);

        public Area TerraformArea { get;  set; }

        /// <summary>
        /// 
        /// Offset for the neighbouring tiles for slope calculation
        /// 
        /// 
        ///         ..
        ///  #  =>  .#
        /// 
        /// </summary>
        private readonly int[] _offsetsToCheck =
        {
            -1, -1,
            0, -1,
            -1, 0,
            0, 0

        };

        protected Position CenterPosition { get; private set; }
       
        protected TerraformingOperation(Position centerPosition,int plantDamage)
        {
            _plantDamage = plantDamage;
            CenterPosition = centerPosition;
        }

        /// <summary>
        /// 
        /// Modifies the area like this:
        /// 
        ///           ....
        ///  ###      .###
        ///  # #  =>  .# # 
        ///  ###      .###
        /// 
        /// Throws if any player in area
        /// 
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="terrain"></param>
        public void Prepare(IZone zone)
        {
            var preparedArea = new Area(TerraformArea.X1 - 1, TerraformArea.Y1 - 1, TerraformArea.X2,TerraformArea.Y2).Clamp(zone.Size);
            zone.Players.WithinArea(preparedArea).Any().ThrowIfTrue(ErrorCodes.PlayerInTerraformArea);
        }

        public virtual void AcceptVisitor(TerraformingOperationVisitor visitor)
        {
            visitor.VisitTerraformingOperation(this);
        }


        /// <summary>
        /// 
        /// Heart of terraforming.
        /// 
        /// checks terrain conditions for the terraforming operation
        /// simply ignores invalid tiles
        /// damages plants if there are any on the area
        /// 
        /// all good => modify terrain
        /// 
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="terrain"></param>
        /// <param name="tileAction"></param>
        protected void ProcessAreaHelper(IZone zone, Action<int, int> tileAction)
        {
            zone.ForEachAreaInclusive(TerraformArea, (x, y) =>
            {
                var controlInfo = zone.Terrain.Controls.GetValue(x, y);
                if (controlInfo.IsAnyTerraformProtected)
                    return;

                var blockingInfo = zone.Terrain.Blocks.GetValue(x, y);
                if (blockingInfo.NonNaturally)
                    return;

                var plantInfo = zone.Terrain.Plants.GetValue(x, y);
                if (plantInfo.type != PlantType.NotDefined)
                {
                    //do plant damage
                    zone.DamageToPlant(x, y, _plantDamage);
                    return;
                }

                tileAction(x, y);
            });
        }

       
        

        public virtual void DoTerraform(IZone zone)
        {
            _bufferArea = TerraformArea.AddBorder(1);
            FillBufferWithCurrentAltitude(zone);
            
            //do the terraforming in a buffer
            TerraformArea.ForEachXY((x, y) =>
            {
                var bufferOffset = CalculateBufferOffset(x, y);
                var originalAltitude = _buffer[bufferOffset];

                var direction = ProduceDirection(zone, x,y);
                if (direction == 0) { return; }

                //makes sure it maximizes the terrain change 
                //not perfect, but the next operation will get closer
                //
                //if the operation fails it tries to use the half of the direction and retries
                while (!TryDoOperationAndCheckAffectedBySlope(zone, direction, x, y))
                {
                    // divide the direction
                    var sign = Math.Sign(direction);
                    var tmp = Math.Abs(direction);
                    direction = (tmp >> 1) * sign;

                    //reset the tile so the next iteration can restart the work
                    _buffer[bufferOffset] = originalAltitude;

                    if (Math.Abs(direction) < 1)
                    {
                        break;
                    }
                }
            });

            //the actual operation is a simple set

            //there it will start checking for terrain conditions and plants
            //and if all good -> do the set
            ProcessAreaHelper(zone,(x, y) =>
            {
                zone.Terrain.Altitude.UpdateValue(x,y,current => _buffer[CalculateBufferOffset(x, y)]);
            });
        }

        private bool TryDoOperationAndCheckAffectedBySlope(IZone zone, int direction, int x, int y)
        {
            var bufferOffset = CalculateBufferOffset(x, y);
            var newAltitude = (ushort)(_buffer[bufferOffset] + direction).Clamp(ushort.MinValue,ushort.MaxValue);

            //write new altitude into the buffer so the surrounding can check it without any exception ifs...
            _buffer[bufferOffset] = newAltitude;

            //check against the affected tiles -> who's slope is affected by this operation?
            for (var i = 0; i <= _offsetsToCheck.Length - 2; i = i + 2)
            {
                var tileToCheckX = x + _offsetsToCheck[i];
                var tileToCheckY = y + _offsetsToCheck[i + 1];

                if (!CheckOneTileForSlope(zone, tileToCheckX, tileToCheckY))
                {
                    return false;
                }

            }

            return true;

        }

        /// <summary>
        /// The wicked filter to check the terraforming slope threshold
        /// 
        /// remember, this is a synced operation per zone
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool CheckOneTileForSlope(IZone zone, int x, int y)
        {
            //get it from the live terrain
            var origSlope = zone.Terrain.Slope.GetValue(x, y);

            //the one we generated
            var newSlope = GetSlopeFromBuffer(x, y);

            //if the new slope is under => good
            if (newSlope <= SLOPE_THRESHOLD)
            {
                return true;
            }

            //if the new slope got smaller, the terrain got flatter than it was => good
            if (newSlope < origSlope)
            {
                return true;
            }
            
            return false;

        }

        private int CalculateBufferOffset(int x, int y)
        {
            return (x - _bufferArea.X1) + (y - _bufferArea.Y1) * _bufferArea.Width;
        }

        private byte GetSlopeFromBuffer(int x, int y)
        {
            var tl_o = CalculateBufferOffset(x, y);
            var tr_o = CalculateBufferOffset(x + 1, y);
            var bl_o = CalculateBufferOffset(x, y + 1);
            var br_o = CalculateBufferOffset(x + 1, y + 1);

            var tl_a = _buffer[tl_o];
            var tr_a = _buffer[tr_o];
            var bl_a = _buffer[bl_o];
            var br_a = _buffer[br_o];

            var slope = CalculateSlopeByAltitude(tl_a, tr_a, bl_a, br_a);

            return slope;

        }

        /// <summary>
        /// Load raw altitude into buffer
        /// </summary>
        private void FillBufferWithCurrentAltitude(IZone zone)
        {
            _buffer = new ushort[_bufferArea.Ground];

            zone.ForEachAreaInclusive(_bufferArea, (x, y) =>
            {
                var bufferOffset = (x - _bufferArea.X1) + (y - _bufferArea.Y1) * _bufferArea.Width;
                var alt = zone.Terrain.Altitude.GetValue(x, y);
                _buffer[bufferOffset] = alt;
            });
        }

        /// <summary>
        /// 
        /// -----------
        /// | tl | tr |
        /// -----------
        /// | bl | br |
        /// -----------
        /// 
        /// </summary>
        /// <param name="tlAltitude"></param>
        /// <param name="trAltitude"></param>
        /// <param name="blAltitude"></param>
        /// <param name="brAltitude"></param>
        /// <returns></returns>
        public byte CalculateSlopeByAltitude(ushort tlAltitude, ushort trAltitude, ushort blAltitude, ushort brAltitude)
        {

            tlAltitude = (ushort)(tlAltitude >> 5);
            trAltitude = (ushort)(trAltitude >> 5);
            blAltitude = (ushort)(blAltitude >> 5);
            brAltitude = (ushort)(brAltitude >> 5);

            var e = (tlAltitude + trAltitude) >> 1;
            var f = (trAltitude + brAltitude) >> 1;
            var g = (brAltitude + blAltitude) >> 1;

            var h = (blAltitude + e) >> 1;
            var i = (tlAltitude + trAltitude + brAltitude + blAltitude) >> 2;

            return (byte)((Math.Abs(i - tlAltitude) +
                           Math.Abs(i - trAltitude) +
                           Math.Abs(i - brAltitude) +
                           Math.Abs(i - blAltitude) +
                           Math.Abs(i - e) +
                           Math.Abs(i - f) +
                           Math.Abs(i - g) +
                           Math.Abs(i - h)) * 2).Clamp(0, 255);
        }
    }
}