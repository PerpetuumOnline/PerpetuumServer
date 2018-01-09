using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.MissionEngine
{
    public enum MissionSpotType
    {
        fieldterminal,
        mswitch,
        kiosk,
        itemsupply,
        randompoint,

        terminal,
        teleport,
        sap
    }


    public class MissionSpotStat : MissionSpot
    {
        private readonly Dictionary<MissionSpotType, int> _selectableSpots = new Dictionary<MissionSpotType, int>(); 

        public MissionSpotStat(MissionSpotType missionSpotType, Position position, int zoneId) : base(missionSpotType, position, zoneId)
        {
        }

        private MissionSpotStat(MissionSpot spot) : base(spot.type,spot.position,spot.zoneId )
        {
        }

        public void SetSelectableSpotAmount(MissionSpotType missionSpotType, int amount)
        {
            _selectableSpots[missionSpotType] = amount;
        }

        public int GetAmountByType(MissionSpotType missionSpotType)
        {
            int amount;
            if (_selectableSpots.TryGetValue(missionSpotType, out amount))
            {
                return amount;
            }

            return 0;
        }




        public static MissionSpotStat CreateFromSpot(MissionSpot spot)
        {
            var mss = new MissionSpotStat(spot);
            return mss;
        }
    }

    public class MissionSpot
    {
        private static MissionDataCache _missionDataCache;
        public static IZoneManager ZoneManager { get; set; }

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }
        
        public MissionSpotType type;
        public Position position;
        public int zoneId;
        public int findRadius;

        public MissionSpot(MissionSpotType missionSpotType, Position position, int zoneId)
        {
            type = missionSpotType;
            this.position = position;
            this.zoneId = zoneId;
        }

        public static MissionSpot FromRecord(IDataRecord record)
        {
            var spottype = (MissionSpotType) record.GetValue<int>("type");
            var x = record.GetValue<int>("x");
            var y = record.GetValue<int>("y");
            var p = new Position(x, y);
            p = p.Center;
            var zoneIdSql = record.GetValue<int>("zoneid");

            var si = new MissionSpot(spottype,p,zoneIdSql);
            return si;
        }

        public IZone Zone => ZoneManager.GetZone(zoneId);

        private void SetFindRadius(int radius)
        {
            this.findRadius = radius;
        }

        public MissionSpotStat CountSelectableSpots(List<MissionSpot> allSpotsOnZone)
        {
            var msst = MissionSpotStat.CreateFromSpot(this);

            msst.SetSelectableSpotAmount(MissionSpotType.randompoint, CountSelectableByType(MissionSpotType.randompoint, allSpotsOnZone));
            msst.SetSelectableSpotAmount(MissionSpotType.mswitch, CountSelectableByType(MissionSpotType.mswitch, allSpotsOnZone));
            msst.SetSelectableSpotAmount(MissionSpotType.kiosk, CountSelectableByType(MissionSpotType.kiosk, allSpotsOnZone));
            msst.SetSelectableSpotAmount(MissionSpotType.itemsupply, CountSelectableByType(MissionSpotType.itemsupply, allSpotsOnZone));

            return msst;
        }

        private int CountSelectableByType(MissionSpotType missionSpotType, List<MissionSpot> spots)
        {
            return spots.Count(s => s.type == missionSpotType &&
                                 position.ToPoint().ToPosition().TotalDistance2D((Point) s.position.ToPoint()) > 0.5 &&
                                 position.IsInRangeOf2D(s.position, s.findRadius) &&
                                 _missionDataCache.IsTargetSelectionValid(Zone, position, s.position));
        }


   

        public void Save()
        {
            var res = Db.Query().CommandText(@"INSERT dbo.missionspotinfo ( type, zoneid, x, y ) VALUES  ( @type, @zoneId, @x,  @y)")
                .SetParameter("@type", (int)type)
                .SetParameter("@zoneId", zoneId)
                .SetParameter("@x", position.intX)
                .SetParameter("@y", position.intY)
                .ExecuteNonQuery();
            (res == 1).ThrowIfFalse(ErrorCodes.SQLInsertError);
        }

        public static List<MissionSpot> LoadByZoneId(int zoneId)
        {
            return
                Db.Query().CommandText("select * from missionspotinfo where zoneid=@zoneId").SetParameter("@zoneId", zoneId).Execute()
                    .Select(FromRecord).ToList();
        }

        public static List<MissionSpot> GetMissionSpotsFromUnitsOnZone(IZone zone, bool collectFindRadius = false)
        {
            var spots = new List<MissionSpot>();

            var fieldTerminals = zone.Units.Where(u => u is FieldTerminal).ToList();

            foreach (var fieldTerminal in fieldTerminals)
            {
                var ms = new MissionSpot(MissionSpotType.fieldterminal, fieldTerminal.CurrentPosition, zone.Id);
                spots.Add(ms);
            }

            var missionStructures = zone.Units.Where(u => u is MissionStructure).Cast<MissionStructure>().ToList();

            foreach (var missionStructure in missionStructures)
            {
                MissionSpotType spotType;

                switch (missionStructure.TargetType)
                {
                    
                    case MissionTargetType.use_itemsupply:
                        spotType = MissionSpotType.itemsupply;
                        break;

                    case MissionTargetType.submit_item:
                        spotType = MissionSpotType.kiosk;
                        break;

                    default:
                        spotType = MissionSpotType.mswitch;
                        break;
                }

                var ms = new MissionSpot(spotType, missionStructure.CurrentPosition, zone.Id);

                if (collectFindRadius)
                {
                    var missionTarget = _missionDataCache.GetTargetByStructureUnit(missionStructure);
                    ms.SetFindRadius(missionTarget.FindRadius);
                }

                spots.Add(ms);
            }

            return spots;
        }

        public static List<MissionSpot> GetRandomPointSpotsFromTargets(ZoneConfiguration zoneConfig, bool collectFindRadius = false)
        {
            var pointTargets = _missionDataCache.GetAllMissionTargets.Where(t => t.Type == MissionTargetType.rnd_point && t.ZoneId == zoneConfig.Id).ToList();

            var result = new List<MissionSpot>(pointTargets.Count);

            foreach (var missionTarget in pointTargets)
            {
                var ms = new MissionSpot(MissionSpotType.randompoint, missionTarget.targetPosition, zoneConfig.Id);

                if (collectFindRadius)
                    ms.SetFindRadius(missionTarget.FindRadius);

                result.Add(ms);
            }

            return result;
        }

        public static Dictionary<MissionSpotType, List<Position>> GetStaticObjectsFromZone(IZone zone)
        {
            var terminals = GetTerminalPositionsFromZone(zone);
            var teleports = zone.Units.Where(u => u.ED.CategoryFlags.IsCategory(CategoryFlags.cf_teleport_column)).Select(t => t.CurrentPosition).ToList();
            var saps = zone.Units.Where(u => u.ED.CategoryFlags.IsCategory(CategoryFlags.cf_outpost)).Cast<Outpost>().SelectMany(o => o.SAPInfos).Select(s => s.Position).ToList();
            var fieldTerminals = zone.Units.Where(u => u.ED.CategoryFlags.IsCategory(CategoryFlags.cf_field_terminal)).Select(t => t.CurrentPosition).ToList();

            var staticObjects = new Dictionary<MissionSpotType, List<Position>>
            {
                {MissionSpotType.terminal, terminals},
                {MissionSpotType.teleport, teleports},
                {MissionSpotType.sap, saps},
                {MissionSpotType.fieldterminal, fieldTerminals}
            };

            return staticObjects;
        }

        private static List<Position> GetTerminalPositionsFromZone(IZone zone)
        {
            return  zone.Units.Where(u => u.ED.CategoryFlags.IsCategory(CategoryFlags.cf_public_docking_base)).Select(t => t.CurrentPosition).ToList();
        }

        public static List<MissionSpot> GenerateMissionSpotsFromPositions(IZone zone, MissionSpotType missionSpotType, List<Position> positions)
        {
            var result = new List<MissionSpot>(positions.Count);

            foreach (var pos in positions)
            {
                var ms = new MissionSpot(missionSpotType, pos, zone.Id);

                result.Add(ms);
            }
            
            return result;
        }

        public static List<MissionSpot> GetTerminalSpotsFromZone(IZone zone)
        {
            var positions = GetTerminalPositionsFromZone(zone);
            return GenerateMissionSpotsFromPositions(zone,MissionSpotType.terminal, positions);
        }


    }

    
}
