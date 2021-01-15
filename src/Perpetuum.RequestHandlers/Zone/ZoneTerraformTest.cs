using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneTerraformTest : IRequestHandler<IZoneRequest>
    {
        private static readonly object _lockObject = new object();

        private ushort[] _buffer;
        private Area _bufferArea;
        private Area _workArea;
        private const int radius = 4;
        private const int SLOPE_THRESHOLD = 19;
        private int origDirection;


        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var character = request.Session.Character;

            var player = request.Zone.GetPlayer(character);

            var pos = request.Zone.GetPrimaryLockedTileOrThrow( character);

            /*
           foreach (var t  in  player.LockHandler.Locks.OfType<TerrainLock>())
           {
               PBSHelper.DegradeTowardsOriginal(_zone, t.Location);
           }
            */

            Task.Run(() => { AreaTestProgramCodeSourceMethod(pos.intX, pos.intY, request);  });

            var info = new Dictionary<string, object> {{k.message, "Thread started be patient!"}};
           
            Message.Builder.FromRequest(request).WithData(info).Send();
        }


        private void AreaTestProgramCodeSourceMethod(int cx , int cy, IZoneRequest request)
        {
            var testArea = Area.FromRadius(cx, cy, 300);
            testArea = testArea.Clamp(request.Zone.Size);

            var counter = 0;
            testArea.ForEachXY((x, y) =>
            {
                if (counter ++ % 2 == 0)
                {
                    PBSHelper.DegradeTowardsOriginal(request.Zone, new Position(x, y));
                }

            });

            var info = new Dictionary<string, object> { { k.message, "Thread finished. Time to fetch altitude!" } };

            Message.Builder.FromRequest(request).WithData(info).Send();
        }


        //szabaly:
        //minden kornyezo csempe csak javulhat, ha egy is romlik akkor ugrik a valtozas

        public void HandleRequest_real(IZoneRequest request)
        {
            var direction = request.Data.GetOrDefault(k.amount, 4);
            origDirection = direction;

            var character = request.Session.Character;
            var player = request.Zone.GetPlayer(character);

            var primaryLock = (TerrainLock)player.GetPrimaryLock();

            var p = primaryLock.Location;

            _workArea = Area.FromRadius(p, radius);
            _bufferArea = _workArea.AddBorder(1);

            lock (_lockObject)
            {
                ExecuteTerraforming(request.Zone,direction);
            }
        }


        private void ExecuteTerraforming(IZone zone,int direction)
        {

            //load terrain into buffer
            FillBufferWithCurrentAltitude(zone);

            //do the terraforming in a buffer
            _workArea.ForEachXY((x, y) =>
            {
                var bufferOffset = CalculateBufferOffset(x, y);
                var originalAltitude = _buffer[bufferOffset];

                while (!TryDoOperationAndCheckAffectedBySlope(zone,direction, x, y))
                {

                    var sign = Math.Sign(direction);

                    var tmp = Math.Abs(direction);

                    direction = (tmp >> 1)*sign;

                    //reset the tile so the next iteration can restart the work
                    _buffer[bufferOffset] = originalAltitude;


                    if (Math.Abs(direction) < 1)
                    {
                        direction = origDirection;
                        break;
                    }

                }

            });

            DisplaySlopesFromBuffer();

            using (new TerrainUpdateMonitor(zone))
            {
                _workArea.ForEachXY((x, y) =>
                {
                    var bufferOffset = CalculateBufferOffset(x, y);
                    zone.Terrain.Altitude.UpdateValue(x,y,origAltitude => _buffer[bufferOffset]);
                });
            }
        }



        private readonly int[] _offsetsToCheck =
        {
            -1, -1,
            0, -1,
            -1, 0,
            0, 0

        };


        private bool TryDoOperationAndCheckAffectedBySlope(IZone zone,int direction, int x, int y)
        {
            var bufferOffset = CalculateBufferOffset(x, y);
            var newAltitude = (ushort) (_buffer[bufferOffset] + direction);

            //write new altitude into the buffer so the surrounding can check it without any exception ifs...
            _buffer[bufferOffset] = newAltitude;


            for (var i = 0; i <= _offsetsToCheck.Length - 2; i = i + 2)
            {
                var tileToCheckX = x + _offsetsToCheck[i];
                var tileToCheckY = y + _offsetsToCheck[i + 1];

                if (!CheckOneTileBySlope(zone,tileToCheckX, tileToCheckY))
                {
                    return false;
                }

            }

            return true;

        }


        private bool CheckOneTileBySlope(IZone zone,int x, int y)
        {
            var origSlope = zone.Terrain.Slope.GetValue(x, y);

            var newSlope = GetSlopeFromBuffer(x, y);

            if (newSlope <= SLOPE_THRESHOLD)
            {
                return true;
            }

            if (newSlope < origSlope)
            {
                return true;
            }

            return false;

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




        private int CalculateBufferOffset(int x, int y)
        {
            return (x - _bufferArea.X1) + (y - _bufferArea.Y1)*_bufferArea.Width;
        }



        private void FillBufferWithCurrentAltitude(IZone zone)
        {
            _buffer = new ushort[_bufferArea.Ground];

            //load terrain into buffer
            zone.ForEachAreaInclusive(_bufferArea, (x, y) =>
            {
                var bufferOffset = (x - _bufferArea.X1) + (y - _bufferArea.Y1)*_bufferArea.Width;
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
        private byte CalculateSlopeByAltitude(ushort tlAltitude, ushort trAltitude, ushort blAltitude, ushort brAltitude)
        {

            tlAltitude = (ushort) (tlAltitude >> 5);
            trAltitude = (ushort) (trAltitude >> 5);
            blAltitude = (ushort) (blAltitude >> 5);
            brAltitude = (ushort) (brAltitude >> 5);

            var e = (tlAltitude + trAltitude) >> 1;
            var f = (trAltitude + brAltitude) >> 1;
            var g = (brAltitude + blAltitude) >> 1;

            var h = (blAltitude + e) >> 1;
            var i = (tlAltitude + trAltitude + brAltitude + blAltitude) >> 2;

            return (byte) ((Math.Abs(i - tlAltitude) +
                            Math.Abs(i - trAltitude) +
                            Math.Abs(i - brAltitude) +
                            Math.Abs(i - blAltitude) +
                            Math.Abs(i - e) +
                            Math.Abs(i - f) +
                            Math.Abs(i - g) +
                            Math.Abs(i - h))*2).Clamp(0, 255);
        }


        private void DisplaySlopesFromBuffer()
        {


            var width = _workArea.Width;
            var counter = 0;
            var lineString = "";

            _workArea.ForEachXY((x, y) =>
            {

                var slope = GetSlopeFromBuffer(x, y);
                lineString += slope.ToString().PadLeft(3, '0') + " ";

                counter++;
                if (counter == width)
                {
                    counter = 0;
                    Console.WriteLine(lineString);
                    lineString = "";
                }



            });

        }
    }
}