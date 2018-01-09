using System.Collections.Generic;
using System.Drawing;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones
{
    public interface IZoneConfigurationReader
    {
        IEnumerable<ZoneConfiguration> GetAll();
    }

    public class ZoneConfigurationReader : IZoneConfigurationReader
    {
        private readonly GlobalConfiguration _globalConfiguration;
        private readonly PlantRuleLoader _plantRuleLoader;

        public ZoneConfigurationReader(GlobalConfiguration globalConfiguration,PlantRuleLoader plantRuleLoader)
        {
            _globalConfiguration = globalConfiguration;
            _plantRuleLoader = plantRuleLoader;
        }

        public IEnumerable<ZoneConfiguration> GetAll()
        {
            var records = Db.Query().CommandText("select * from zones where enabled = 1").Execute();

            var result = new List<ZoneConfiguration>();

            var port = _globalConfiguration.ListenerPort + 1;

           

            foreach (var record in records)
            {
                var x = record.GetValue<int>("x");
                var y = record.GetValue<int>("y");
                var w = record.GetValue<int>("width");
                var h = record.GetValue<int>("height");
                var id = record.GetValue<int>("id");

                var config = new ZoneConfiguration
                {
                    Id = id,
                    WorldPosition = new Point(x, y),
                    Size = new Size(w, h),
                    Name = record.GetValue<string>("name"),
                    Fertility = record.GetValue<int>("fertility"),
                    PluginName = record.GetValue<string>("zoneplugin"),
                    //ListenerAddress = "127.0.0.1",
                    ListenerPort = port++,
                    NpcSpawnId = record.GetValue<int?>("spawnid") ?? 0,
                    Protected = record.GetValue<bool>("protected"),
                    Terraformable = record.GetValue<bool>("terraformable"),
                    RaceId = record.GetValue<int>("raceid"),
                    plantRuleSetId = record.GetValue<int>("plantruleset"),
                    Type = (ZoneType)record.GetValue<int>("zonetype"),
                    SparkCost = record.GetValue<int>("sparkcost"),
                    MaxPlayers = 10000,
                    MaxDockingBase = record.GetValue<int>("maxdockingbase"),
                    PlantAltitudeScale = record.GetValue<double>("plantaltitudescale"),
                };

                config.PlantRules = _plantRuleLoader.LoadPlantRulesWithOverrides(config.plantRuleSetId);
                result.Add(config);
            }

            return result;
        }
    }

    public sealed class ZoneConfiguration
    {
        public static readonly ZoneConfiguration None = new ZoneConfiguration { Id = -1 };
        private const int WATER_LEVEL = 55;

        private static readonly Dictionary<int, string> _raceIDToTeleport = new Dictionary<int, string>
        {
            {1, DefinitionNames.PUBLIC_TELEPORT_COLUMN_PELISTAL},
            {2, DefinitionNames.PUBLIC_TELEPORT_COLUMN_NUIMQOL},
            {3, DefinitionNames.PUBLIC_TELEPORT_COLUMN_THELODICA}
        };
        
        public int plantRuleSetId;

        public int Id { get; set; }
        public Size Size { get; set; }
        public Point WorldPosition { get; set; }
        public string PluginName { get; set; }
        public int Fertility { get; set; }
        public bool Terraformable { get; set; }
        public bool Protected { get; set; }
        public int NpcSpawnId { get; set; }
        public int MaxPlayers { get; set; }
        public int SparkCost { get; set; }
        public int MaxDockingBase { get; set; }
        public double PlantAltitudeScale { private get; set; }
        public int RaceId { get; set; }

        public string ListenerAddress { get; set; }
        public int ListenerPort { get; set; }

        public ZoneType Type { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.zone, Id},
                {k.position, WorldPosition},
                {k.name, Name},
                {k.fertility, Fertility},
                {k.zonePlugin, PluginName},
                {k.zoneIP,ListenerAddress},
                {k.zonePort,ListenerPort},
                {k.instance, false},
                {k.spawnID, NpcSpawnId},
                {k.plantRuleSetID, plantRuleSetId},
                {k.isProtected, Protected},
                {k.raceID, RaceId},
                {k.width, Size.Width},
                {k.height, Size.Height},
                {k.terraformable, Terraformable},
                {k.waterLevel, WaterLevel},
                {k.type, (int) Type},
                {k.sparkCost, SparkCost},
                {k.maxDockingBase, MaxDockingBase}
            };
        }

        public override string ToString()
        {
            return $"Id:{Id} Name:{Name} Protected:{Protected} Terraformable:{Terraformable}";
        }

        public EntityDefault TeleportColumn
        {
            get
            {
                var name = _raceIDToTeleport.GetOrDefault(RaceId, DefinitionNames.PUBLIC_TELEPORT_COLUMN_PELISTAL);
                return EntityDefault.GetByName(name);
            }
        }

        public string Name { get; set; }

        public ZoneStorage GetStorage()
        {
            return ZoneStorage.Get(this);
        }

        public List<PlantRule> PlantRules { get; set; }

        public static int WaterLevel => WATER_LEVEL;

        public bool IsAlpha => Protected;

        public bool IsBeta => (!Protected && !Terraformable);

        public bool IsGamma => Terraformable;
    }
}

    
