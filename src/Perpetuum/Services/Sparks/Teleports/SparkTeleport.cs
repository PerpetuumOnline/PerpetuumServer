using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.Sparks.Teleports
{
    public class SparkTeleport
    {
        public const int SPARK_TELEPORT_USE_FEE = 1000;
        public const int SPARK_TELEPORT_PLACE_FEE = 10000;

        public int ID;
        public DockingBase DockingBase { get; set; }
        public Character Character;

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.ID, ID},
                    {k.baseEID, DockingBase.Eid},
                    {k.definition, DockingBase.ED.Definition},
                    {k.zoneID, DockingBase.Zone.Id},
                    {k.x, (int) DockingBase.CurrentPosition.X},
                    {k.y, (int) DockingBase.CurrentPosition.Y},
                    {k.dockingBase, DockingBase.ToDictionary()}
                };
        }
    }
}
