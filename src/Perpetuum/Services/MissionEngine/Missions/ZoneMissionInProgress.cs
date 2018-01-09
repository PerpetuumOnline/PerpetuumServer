using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Services.MissionEngine.Missions
{
    public class ZoneMissionInProgress
    {
        private readonly IZone _zone;
        public Guid missionGuid;
        public readonly int missionId;
        public int currentTargetOrder; //current progress of the mission - "which target ids are the current ones?"
        private readonly int? _missionLevel;
        private readonly int? _locationId;
        
        public int MissionLevel { get { return _missionLevel ?? 0; }}
        private int LocationId { get { return _locationId ?? 0; } }

        public readonly int selectedRace;
        public readonly bool spreadInGang;


        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        private static ZoneMissionInProgress CreateFromRecord(IZone zone,IDataRecord record)
        {
           var missionGuid = record.GetValue<Guid>("missionGuid");
           var missionId = record.GetValue<int>("missionID");
           var currentTargetOrder = record.GetValue<int>("grouporder");
           var missionLevel = record.GetValue<int?>("missionlevel");
           var locationId = record.GetValue<int?>("locationid") ?? 0;
           var selectedRace = record.GetValue<int?>("selectedRace") ?? 1;
            var spreadInGang = record.GetValue<bool>("spreadingang");

           return new ZoneMissionInProgress(zone, missionGuid, missionId, currentTargetOrder, missionLevel, locationId, selectedRace, spreadInGang);

        }

        public static IList<ZoneMissionInProgress> GetRunningMissionsSql(IZone zone, int characterId)
        {
            var records = Db.Query().CommandText("SELECT * FROM dbo.missionlog WHERE finished IS NULL and characterID=@characterID and (expire is null or expire > getdate())")
                .SetParameter("@characterID", characterId)
                .Execute();

            return records.Select(r => CreateFromRecord(zone, r)).ToList();
        }


        public static ZoneMissionInProgress CreateFromProgressUpdate(IZone zone,MissionProgressUpdate missionProgressUpdate)
        {
            var missionGuid = missionProgressUpdate.missionGuid;
            var missionId = missionProgressUpdate.missionId;
            var currentTargetOrder = missionProgressUpdate.targetOrder;
            var missionLevel = missionProgressUpdate.missionLevel;
            var locationId = missionProgressUpdate.locationId;
            var selectedRace = missionProgressUpdate.selectedRace;
            var spreadInGang = missionProgressUpdate.spreadInGang;

            return new ZoneMissionInProgress(zone, missionGuid, missionId, currentTargetOrder, missionLevel, locationId, selectedRace,spreadInGang);
        }


        private ZoneMissionInProgress(IZone zone,Guid missionGuid, int missionId, int currentTargetOrder, int? missionLevel, int locationId,  int selectedRace, bool spreadInGang)
        {
            this.missionGuid = missionGuid;
            this.missionId = missionId;
            this.currentTargetOrder = currentTargetOrder;
            _missionLevel = missionLevel;
            _locationId = locationId;
            _zone = zone;
            this.selectedRace = selectedRace;
            this.spreadInGang = spreadInGang;


        }
        

        public override string ToString()
        {
            return $"MissionId: {missionId}, TargetOrder: {currentTargetOrder}";
        }

        public MissionLocation GetLocation
        {
            get { return _missionDataCache.GetLocation(LocationId); }

        }

        /// <summary>
        /// Load targets that handled by the zone
        /// </summary>
        /// <param name="presenceManager"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public IEnumerable<IZoneMissionTarget> LoadZoneTargets(IPresenceManager presenceManager,Player player)
        {
            
            var records =
                Db.Query().CommandText("select * from missiontargetsarchive where characterid=@characterID and missionid=@missionID and missionguid=@guid")
                    .SetParameter("@characterID", player.Character.Id)
                    .SetParameter("@missionID", missionId)
                    .SetParameter("@guid", missionGuid)
                    .Execute().ToList();

            foreach (var record in records)
            {

                var completed = record.GetValue<bool>("completed");
                if (completed) continue;

                var targetId = record.GetValue<int>("targetid");

                //original from cache
                MissionTarget targetFromCache;
                if (!_missionDataCache.GetTargetById(targetId, out targetFromCache))
                {
                    continue;
                }

                //duplicate and apply running parameters
                var target = targetFromCache.GetClone();
                target.ModifyWithRecord(record);
                

                if (target.ValidZoneSet && target.ZoneId != _zone.Configuration.Id)
                {
                    //not for this zone
                    continue;
                }

                //this is the current progress
                var currentProgress = record.GetValue<int>("progresscount");

                Logger.Info("mission target progress: " + currentProgress + " targetID:" + target.id + " missionID:" + missionId + " characterID:" + player.Character.Id + " " + target.Type);

                //these types are not needed on the zone
                switch (target.Type)
                {
                    case MissionTargetType.research:
                    case MissionTargetType.massproduce:
                    case MissionTargetType.prototype:
                    case MissionTargetType.fetch_item:
                    case MissionTargetType.teleport:
                        continue;
                }

                IZoneMissionTarget zoneTarget = null;

                //generate proper class
                switch (target.Type)
                {
                    case MissionTargetType.reach_position:
                        zoneTarget = new ReachPositionZoneTarget(_zone, player, target, this);
                        break;

                    case MissionTargetType.loot_item:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new LootZoneTarget(_zone, player, target,  this,pc);
                        break;

                    }
                        
                    case MissionTargetType.kill_definition:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new KillZoneTarget(_zone, player, target, this, pc);
                        break;
                    }

                    case MissionTargetType.scan_mineral:
                        zoneTarget = new ScanMaterialZoneTarget(_zone, player, target, this);
                        break;

                    case MissionTargetType.scan_unit:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new ScanUnitZoneTarget(_zone, player, target, this,pc);
                        break;
                    }

                    case MissionTargetType.scan_container:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new ScanContainerZoneTarget(_zone, player, target, this,pc);
                        break;
                    }

                    case MissionTargetType.drill_mineral:
                    {
                        var pc = new ProgressCounter(currentProgress,target.Quantity);
                        zoneTarget = new DrillMineralZoneTarget(_zone, player, target, this, pc);
                        break;
                    }

                    case MissionTargetType.submit_item:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new SubmitItemZoneTarget(_zone, player, target, this,pc);
                        break;
                    }

                    case MissionTargetType.use_switch:
                        zoneTarget = new AlarmSwitchZoneTarget(_zone, player, target, this);
                        break;

                    case MissionTargetType.find_artifact:
                        zoneTarget = new FindArtifactZoneTarget(_zone, player, target, this,presenceManager);
                        break;
                    

                    case MissionTargetType.use_itemsupply:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new ItemSupplyZoneTarget(_zone, player, target, this, pc);
                        break;
                    }

                    case MissionTargetType.harvest_plant:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new HarvestPlantZoneTarget(_zone, player, target, this, pc);
                        break;
                    }

                    case MissionTargetType.summon_npc_egg:
                        zoneTarget = new SummonNpcEggZoneTarget(_zone, player, target, this);
                        break;

                    case MissionTargetType.pop_npc:
                        zoneTarget = new PopNpcZoneTarget(_zone, player, target, this,presenceManager);
                        break;

                    case MissionTargetType.lock_unit:
                    {
                        var pc = new ProgressCounter(currentProgress, target.Quantity);
                        zoneTarget = new LockUnitZoneTarget(_zone, player, target, this, pc);
                        break;
                    }
                        
                }

                if (zoneTarget == null)
                {
                    Logger.Error( "no class defined for:" + target.Type);
                    continue;
                }

                yield return zoneTarget;
            }
        }

        public void SetCurrentTargetOrder(MissionProgressUpdate missionProgressUpdate)
        {
            Debug.Assert(currentTargetOrder + 1 == missionProgressUpdate.targetOrder, " updated grouporder has a problem!!! ");

            currentTargetOrder = missionProgressUpdate.targetOrder;

        }

        public Mission GetMission
        {
            get { return _missionDataCache.GetMissionById(missionId); }
        }
    }
}
