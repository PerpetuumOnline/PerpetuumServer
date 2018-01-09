using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.Standing;
using Perpetuum.Zones;

namespace Perpetuum.Services.MissionEngine
{
    public class MissionHelper
    {
        private static MissionDataCache _missionDataCache;
        private static IStandingHandler _standingHandler;
        public static MissionProcessor MissionProcessor { get; set; }
        public static IEntityServices EntityServices { get; set; }

        public static void Init(MissionDataCache missionDataCache,IStandingHandler standingHandler)
        {
            _missionDataCache = missionDataCache;
            _standingHandler = standingHandler;
        }

        public static void GetStandingData(Character character, long allianceEid, out double standing, out int missionLevel)
        {
            standing = _standingHandler.GetStanding(allianceEid, character.Eid);
            //0.1->0  1.0->1  1.1->1  -2.3->0
            missionLevel = (int)(Math.Floor(standing).Clamp(0, double.MaxValue));
        }

        public static void MissionAdvanceDockInTarget(int characterId, int zoneId, Position position)
        {
            var data = new Dictionary<string, object>
            {
                {k.type, MissionTargetType.dock_in},
                {k.zoneID, zoneId},
                {k.position, position},
                {k.characterID, characterId},
                {k.completed, true},
            };

            MissionProcessor.EnqueueMissionTargetAsync(data);
            Logger.Info(">>>>>> dock in mission target sent to mission engine for character: " + characterId + " " + zoneId + " position: " + position.ToDoubleString2D());
        }

        /// <summary>
        /// converting from old mission structure targets. writing eid to a new column
        /// </summary>
        /// <param name="missionTarget"></param>
        private static void FindMyStructure(MissionTarget missionTarget)
        {
           
            var switchDefinitions = EntityServices.Defaults.GetAll().GetDefinitionsByCategoryFlag(CategoryFlags.cf_mission_switch);
            var kioskDefinitions = EntityServices.Defaults.GetAll().GetDefinitionsByCategoryFlag(CategoryFlags.cf_kiosk);
            var itemSupplyDefinitions = EntityServices.Defaults.GetAll().GetDefinitionsByCategoryFlag(CategoryFlags.cf_item_supply);

            string defz;
            switch (missionTarget.Type)
            {
                case MissionTargetType.submit_item:
                    defz = kioskDefinitions.ArrayToString();
                    break;
                case MissionTargetType.use_switch:
                    defz = switchDefinitions.ArrayToString();
                    break;
                case MissionTargetType.use_itemsupply:
                    defz = itemSupplyDefinitions.ArrayToString();
                    break;
                default:
                    throw new Exception("mijen type? " + missionTarget);
            }


            var xMin = missionTarget.targetPosition.X - missionTarget.TargetPositionRange;
            var xMax = missionTarget.targetPosition.X + missionTarget.TargetPositionRange;
            var yMin = missionTarget.targetPosition.Y - missionTarget.TargetPositionRange;
            var yMax = missionTarget.targetPosition.Y + missionTarget.TargetPositionRange;

            var q = "SELECT eid FROM dbo.zoneentities WHERE zoneID=@zoneId AND @xmin < x AND x < @xmax AND @ymin < y AND y < @ymax and definition in (" + defz + ")";

            var eidList =
                Db.Query().CommandText(q)
                .SetParameter("@zoneid", missionTarget.ZoneId)
                    .SetParameter("@xmin", xMin)
                    .SetParameter("@xmax", xMax)
                    .SetParameter("@ymin", yMin)
                    .SetParameter("@ymax", yMax)
                    .Execute()
                    .Select(r => r.GetValue<long>(0))
                    .ToList();





            if (eidList.IsNullOrEmpty())
            {
                Logger.Error("no structure was found for mission target: " + missionTarget);
                return;
            }

            if (eidList.Count == 1)
            {
                var structureEid = eidList.First();
                Logger.Info("exactly one structure was found " + structureEid);
                
                Db.Query().CommandText("update missiontargets set structureeid=@eid where id=@id")
                    .SetParameter("@id", missionTarget.id)
                    .SetParameter("@eid", structureEid)
                    .ExecuteNonQuery();




                return;
            }

            Logger.Warning("multiple items were found " + missionTarget);

        }

        /// <summary>
        /// helper stuff
        /// 
        /// utoljara arra hasznaltam h visszaszedjek eidket, de at kell irni olyanra ami kell mindig
        /// </summary>
        public static void ForEachTargetDoSomethingByType()
        {
            foreach (var missionTarget in _missionDataCache.GetAllMissionTargets)
            {

                switch (missionTarget.Type)
                {
                    case MissionTargetType.fetch_item:
                        break;
                    case MissionTargetType.loot_item:
                        break;
                    case MissionTargetType.reach_position:
                        break;
                    case MissionTargetType.kill_definition:
                        break;
                    case MissionTargetType.scan_mineral:
                        break;
                    case MissionTargetType.scan_unit:
                        break;
                    case MissionTargetType.scan_container:
                        break;
                    case MissionTargetType.drill_mineral:
                        break;
                    case MissionTargetType.submit_item:
                    case MissionTargetType.use_switch:
                    case MissionTargetType.use_itemsupply:
                        FindMyStructure(missionTarget);
                        break;
                    case MissionTargetType.find_artifact:
                        break;
                    case MissionTargetType.dock_in:
                        break;

                    case MissionTargetType.prototype:
                        break;
                    case MissionTargetType.massproduce:
                        break;
                    case MissionTargetType.research:
                        break;
                    case MissionTargetType.teleport:
                        break;
                    case MissionTargetType.harvest_plant:
                        break;
                    case MissionTargetType.summon_npc_egg:
                        break;
                    case MissionTargetType.pop_npc:
                        break;
                    case MissionTargetType.rnd_pop_npc:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
        }

        public static void InsertMissionTargetTypeToSql()
        {
            //foreign key miatt nem lehet
            //DbQuery.Create("truncate table missiontargettypes").ExecuteNonQuery();

            var names = Enum.GetNames(typeof(MissionTargetType));
            var values = Enum.GetValues(typeof(MissionTargetType));


            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var value = (int)values.GetValue(i);

                var nameInSql = Db.Query().CommandText("select name from missiontargettypes where id=@value").SetParameter("@value", value).ExecuteScalar<string>();

                if (nameInSql.IsNullOrEmpty())
                {

                    Db.Query().CommandText("insert missiontargettypes (id,name) values (@id,@name)")
                        .SetParameter("@id", value)
                        .SetParameter("@name", name)
                        .ExecuteNonQuery();
    
                }
                else
                {
                    if (name != nameInSql)
                    {
                        Db.Query().CommandText("update missiontargets set name=@name where id=@value")
                            .SetParameter("@name", name)
                            .SetParameter("@value", value)
                            .ExecuteNonQuery();
                    }

                }                

            }
        }

        public static void InsertDynamicArtifacts()
        {
            var query = @"
INSERT dbo.artifacttypes
        ( name, goalrange, npcpresenceid, persistent, minimumloot, [dynamic] )
VALUES  ( @name,  10, NULL, 0, 1, 1 )
";

            for (var i = 1; i <= 99; i++)
            {
                var name = "artifact_dynamic_" + i.ToString(CultureInfo.InvariantCulture);

                try
                {
                    Db.Query().CommandText(query).SetParameter("@name", name).ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Warning("biztos mar van");
                    Logger.Exception(ex);
                }


            }



        }


        public static bool FindIndustrialMissionGuidWithConditions(Character character,  int cprgDefinition, out Guid missionGuid, bool unfinishedOnly = true)
        {
            var query = "SELECT missionguid FROM dbo.missiontargetsarchive WHERE definition=@definition1  AND characterid=@characterId";

            if (unfinishedOnly)
            {
                query += " AND completed=0";
            }

            var result =
            Db.Query().CommandText(query)
                .SetParameter("@targetType1", (int) MissionTargetType.research)
                .SetParameter("@targetType2", (int)MissionTargetType.massproduce)
                .SetParameter("@definition1", cprgDefinition)
                .SetParameter("@characterId", character.Id)
                .ExecuteSingleRow();

            if (result == null)
            {
                missionGuid = Guid.Empty;
                
                return false;
            }

            missionGuid = result.GetValue<Guid>("missionguid");
            
            return true;
        }

        [CanBeNull]
        public static MissionInProgress ReadMissionInProgressByGuid(Guid guid, Character character)
        {
            var record = Db.Query().CommandText("select * from missionlog where missionguid=@guid and characterid=@characterId")
                .SetParameter("@guid", guid)
                .SetParameter("@characterId", character.Id)
                .ExecuteSingleRow();

            if (record == null)
            {
                return null;
            }

            var missionId = record.GetValue<int>("missionID");

            Mission mission;
            if (!_missionDataCache.GetMissionById(missionId, out mission))
            {
                return null;
            }

            return ReadMissionInProgressByRecord(record, mission);


        }

        public static MissionInProgress ReadMissionInProgressByRecord(IDataRecord record, Mission mission)
        {
            var missionInProgress = MissionInProgress.CreateFromRecord(record, mission);

            ErrorCodes ec;
            //load targets from sql and spawn running target classes
            if ((ec = missionInProgress.ReadTargetsInProgressSql()) != ErrorCodes.NoError)
            {
                Logger.Error("error occured loading the targets from sql: " + ec);
                return null;
            }

            return missionInProgress;
        }

        public static void RenumberDisplayOrders()
        {
            using (var scope = Db.CreateTransaction())
            {

                foreach (var mission in _missionDataCache.GetAllMissions)
                {
                    var index = 0;
                    foreach (var target in mission.Targets.OrderBy(t => t.displayOrder))
                    {
                        if (target.displayOrder != index)
                        {
                            var res =
                            Db.Query().CommandText("update missiontargets set displayorder=@index where id=@id")
                                .SetParameter("@index", index)
                                .SetParameter("@id", target.id)
                                .ExecuteNonQuery();

                            (res == 1).ThrowIfFalse(ErrorCodes.SQLUpdateError);

                            Logger.Info("displayOrder corrected " + target.id + " " + target.Type + " " + target.displayOrder +" => "+ index);

                        }

                        index++;

                    }

                }

                scope.Complete();
            }

        }


        public static void PlaceRandomPoint(IZone zone, Position position, int findRadius )
        {
            var x = position.X;
            var y = position.Y;
            
            var name = MissionTarget.GenerateName(MissionTargetType.rnd_point, zone.Id, "spot");
            MissionTarget.InsertMissionTargetSpot(name, MissionTargetType.rnd_point, x, y, zone.Id, findRadius);

        }

        public static void UpdateMissionStructure(IZone zone, long structureEid, int orientation = -1, Position position = new Position())
        {
            var unit = zone.GetUnitOrThrow(structureEid);

            if (!position.Equals(default(Position)))
            {
                position = zone.FixZ(position);
                unit.CurrentPosition = position;

                var mstructure = unit as MissionStructure;
                if (mstructure != null)
                {
                    MissionTarget.UpdatePositionByStructureEid(structureEid,position);
                }
            }

            if (orientation != -1)
            {
                unit.Orientation = (orientation % (byte.MaxValue+1)) / (double)byte.MaxValue;
            }

            zone.UnitService.UpdateDefaultUnit(unit);
        }


        public static void GenerateMissionAtLocationReport(int zoneId)
        {

            var randomCategories = new MissionCategory[]
            {
                 MissionCategory.Combat, MissionCategory.Transport, MissionCategory.Exploration, MissionCategory.Harvesting,
                MissionCategory.Mining, MissionCategory.Production, MissionCategory.CombatExploration, MissionCategory.ComplexProduction
            };

            var locationsOnZone = _missionDataCache.GetAllLocations.Where(l => l.ZoneConfig.Id == zoneId).ToList();
            var allmissions = _missionDataCache.GetAllLiveRandomMissionTemplates.ToList();

            foreach (var category in randomCategories)
            {
                var randomMissions=  allmissions.Where(m => m.missionCategory == category).ToList();

                var ids = randomMissions.Select(m => m.id).ArrayToString();
                Logger.Info("----------------------");
                Logger.Info(randomMissions.Count + " missions in category " + category);

                    foreach (var missionLocation in locationsOnZone)
                    {
                        var qs = "select sum(uniquecases) from missiontolocation where missionid in (" + ids + ") and locationid=@locationID";

                        var count =
                        Db.Query().CommandText(qs).SetParameter("@locationID", missionLocation.id)
                            .ExecuteScalar<int>();

                        Logger.Info(count + " out of " + randomMissions.Count + " at " + missionLocation.id);

                    }
            }
        }

        public static Character FindMissionOwnerByGuid(Guid missionGuid)
        {
            var id = Db.Query().CommandText("select characterid from missiontargetsarchive where missionguid=@guid")
                .SetParameter("@guid", missionGuid)
                .ExecuteScalar<int>();

            return Character.Get(id);

        }
    }
}
