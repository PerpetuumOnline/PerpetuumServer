using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.Environments
{
    [Serializable]
    public struct Tile
    {
        public int x;
        public int y;
        public byte data;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.x, x},
                    {k.y, y},
                    {k.data, (int) data}
                };
        }

        public static Tile FromDictionary(Dictionary<string, object> dictionary)
        {
            return new Tile
            {
                x = (int)dictionary[k.x],
                y = (int)dictionary[k.y],
                data = (byte)(int)dictionary[k.data]
            };
        }

        public Position ToPosition()
        {
            return new Position(x, y);
        }
    }


    public class ZoneEnvironmentHandler : IEnvironmentHandler
    {
        private readonly IZone _zone;

        public ZoneEnvironmentHandler(IZone zone)
        {
            _zone = zone;
        }

        public ErrorCodes SampleEnvironment(long eid, int range, out Dictionary<string, object> verboseResult)
        {
            ErrorCodes ec;
            EntityEnvironmentDescription environmentDescription;
            verboseResult = new Dictionary<string, object>();

            int definition;
            if ((ec = CollectEnvironmentData(eid, range, out definition, out environmentDescription)) != ErrorCodes.NoError)
            {
                return ec;
            }

            if (environmentDescription.blocksTiles == null || environmentDescription.blocksTiles.Count == 0)
            {
                return ErrorCodes.NoError;
            }


            EntityEnvironment.WriteEnvironmentToSql(definition, environmentDescription);
            verboseResult.Add(k.blocks, environmentDescription.blocksTiles.Count);
            return ec;
        }

        private ErrorCodes CollectEnvironmentData(long eid, int range, out int definition, out EntityEnvironmentDescription result)
        {
            result = new EntityEnvironmentDescription();
            const ErrorCodes ec = ErrorCodes.NoError;
            definition = 0;

            var unit = _zone.GetUnit(eid);
            if (unit == null)
                return ErrorCodes.ItemNotFound;

            definition = unit.Definition;

            var entityPosition = unit.CurrentPosition;

            var sampleArea = _zone.CreateArea(entityPosition, range);

            var turns = (int)(Math.Round(unit.Orientation,2) / 0.25);

            var blocksTiles = CollectBlockingHeight(sampleArea, entityPosition, turns);

            if (blocksTiles.Count > 0)
            {
                result.blocksTiles = blocksTiles;
            }

            return ec;
        }


        private List<Tile> CollectBlockingHeight(Area sampleArea, Position origin, int turns)
        {
            var blocksTiles = new List<Tile>();

            for (var j = sampleArea.Y1; j <= sampleArea.Y2; j++)
            {
                for (var i = sampleArea.X1; i <= sampleArea.X2; i++)
                {
                    //collect blocks data
                    var blockingInfo = _zone.Terrain.Blocks[i, j];
                    if (blockingInfo.Height <= 0 || (!blockingInfo.Obstacle && !blockingInfo.Decor)) 
                        continue;

                    var sampledPosition = new Position(i - origin.intX, j - origin.intY);

                    var rotatedRelativePosition = Position.RotateCCWWithTurns(sampledPosition, turns);

                    blocksTiles.Add(new Tile
                    {
                        x = rotatedRelativePosition.intX,
                        y = rotatedRelativePosition.intY,
                        data = (byte) blockingInfo.Height
                    });
                }
            }

            return blocksTiles;
        }

        public List<int> ListEnvironmentDescriptions()
        {
            return Db.Query().CommandText("select definition from environmentdescription")
                          .Execute().Select(r => r.GetValue<int>(0)).ToList();
        }
       

        public ErrorCodes CollectEnvironmentFromPosition(Position origin, int range, int turns, out EntityEnvironmentDescription result)
        {
            result = new EntityEnvironmentDescription();

            var sampleArea = _zone.CreateArea(origin, range);

            var blocksTiles = CollectBlockingHeight(sampleArea, origin, turns);

            if (blocksTiles.Count > 0)
            {
                result.blocksTiles = blocksTiles;
            }

            return ErrorCodes.NoError;
        }
    }
}