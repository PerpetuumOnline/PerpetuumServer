using System.Collections.Generic;

namespace Perpetuum.Zones
{
    public class DistanceConstants
    {
        public const double TERRAIN_DEGRADE_DISTANCE_FROM_PBS = 75.0;
        
        public const double KIOSK_USE_DISTANCE = 3.0;
        public const double SWITCH_USE_DISTANCE = 3.0;
        public const double ITEMSUPPLY_USE_DISTANCE = 3.0;
       
        public const double ALARMSWITCH_STAYCLOSE_DISTANCE = 14.0;
        public const double ITEM_SHOP_DISTANCE = 3.0;
        public const double AREA_BOMB_DISTANCE_TO_STATIONS = 90.0;
        public const double AREA_BOMB_DISTANCE_TO_TELEPORTS = 90.0;
        public const double NEURALYZER_RANGE = 1650;
        public const double ARENA_GUARD_RANGE = 100;
        public const double MOBILE_TELEPORT_USE_RANGE = 8.0;
        public const double INTRUSION_SAP_DEFENSE_RANGE = 100.0;
        public const double PLANT_MIN_DISTANCE_FROM_SAP = 40;
        
        public const double MAX_TERRAFORM_LEVEL_DIFFERENCE = 8.0;
        public const double MAX_TERRAFORM_ALTITUDE_PLAYER_VS_TARGET_DIFFERENCE = 20.0;
        public const double MOBILE_WORLD_TELEPORT_RANGE = 3072;
        public const double PROXIMITY_PROBE_DEPLOY_RANGE = 100.0;
        public const double PLANT_MIN_DISTANCE_FROM_BASE = 100.0;
        public const double PLANT_MAX_DISTANCE_FROM_OUTPOST = 300.0;

        public const double PLANT_MIN_DISTANCE_FROM_PBS = 5.0;
        public const double PLANT_MAX_DISTANCE_FROM_PBS = 150.0;
        
        public const double NPC_EGG_DISTANCE_ON_PVE_ZONES = 50.0;
        public const double PBS_NODE_USE_DISTANCE = 10.0;
        public const double TERRAFORM_MIN_RANGE_FROM_OBJECTS = 25.0;
        public const double TERRAFORM_BUOY_RADIUS = 15;

        public const double MINERAL_DISTANCE_FROM_BASE_MIN = 100;
        public const double EXCLUDED_TELEPORT_RANGE = 200;

        public const double MOBILE_TELEPORT_MIN_DISTANCE_TO_DOCKINGBASE = 50;
        public const double MOBILE_TELEPORT_MIN_DISTANCE_TO_TELEPORT = 50;

        public const double FIELD_TERMINAL_USE = 7.0;
        public const double MISSION_STRUCTURE_FINDRADIUS_DEFAULT = 200;
        public const double MISSION_RANDOM_POINT_FINDRADIUS_DEFAULT = 100;

        public const double MAX_NPC_FLOCK_HOME_RANGE = 40;

        public static Dictionary<string, object> GetEnumDictionary()
        {
            var result = new Dictionary<string, object>();

            var type = typeof (DistanceConstants); // Get type pointer
            var fields = type.GetFields(); // Obtain all fields
            foreach (var field in fields) // Loop through fields
            {
                var name = field.Name; // Get string name
                var temp = field.GetValue(null); // Get value
                
                if (temp is double)
                {
                    result.Add(name, temp);
                }

            }
            return result;
        }
    }
}
