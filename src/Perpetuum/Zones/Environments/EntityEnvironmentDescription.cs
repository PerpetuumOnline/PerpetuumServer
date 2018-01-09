using System.Collections.Generic;

namespace Perpetuum.Zones.Environments
{
    public struct EntityEnvironmentDescription
    {
        public List<Tile> blocksTiles;

        public Dictionary<string, object > ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.blocks, blocksTiles.ToDictionary("t", t => t.ToDictionary())},
                       };
        }
    }
}
