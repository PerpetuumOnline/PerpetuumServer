using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.Scanning;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    public class LootMissionEventInfo : MissionEventInfo
    {
        public Item LootedItem { get; private set; }
        public Point LootedPosition { get; private set; }
        public Guid MissionGuid { get; private set; }
        public int DisplayOrder { get; private set; }

        public LootMissionEventInfo(Player player, Item lootedItem, Point lootedPosition, Guid missionGuid, int displayOrder) : base(player)
        {
            LootedItem = lootedItem;
            LootedPosition = lootedPosition;
            MissionGuid = missionGuid;
            DisplayOrder = displayOrder;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.loot_item;}
        }

        public override Position Position
        {
            get { return LootedPosition.ToPosition(); }
        }

        public override bool IsDefinitionMatching(IZoneMissionTarget missionTarget)
        {
            return missionTarget.MyTarget.Definition == LootedItem.Definition;
        }
    }


    public class LootZoneTarget : ZoneMissionTarget<LootMissionEventInfo>
    {
        private readonly ProgressCounter _progressCounter;

        public LootZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override void OnHandleMissionEvent(LootMissionEventInfo e)
        {
            _progressCounter.Current += e.LootedItem.Quantity;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
            }

            this.SendReportToMissionEngine();    
        }

        protected override bool CanHandleMissionEvent(LootMissionEventInfo e)
        {
            if (MyTarget.Definition != e.LootedItem.Definition)
                return false;

            if (!IsZoneOrPositionValid(e.LootedPosition.ToPosition()))
                return false;

            var isGenericRandomItem = EntityDefault.Get(MyTarget.Definition).CategoryFlags.IsCategory(CategoryFlags.cf_generic_random_items);
            
            var myMission = MyZoneMissionInProgress.GetMission;
            if (myMission.behaviourType == MissionBehaviourType.Random && isGenericRandomItem)
            {
                if (e.MissionGuid != MyZoneMissionInProgress.missionGuid)
                    return false;

                if (e.DisplayOrder != MyTarget.displayOrder)
                    return false;
            }

            return true;
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.definition, MyTarget.Definition);
            _progressCounter.AddToDictionary(result);
            return result;
        }

    }

    public class ReachPositionEventInfo : MissionEventInfo
    {
        public Point ReachedPoint { get; private set; }

        public ReachPositionEventInfo(Player player, Point reachedPoint) : base(player)
        {
            ReachedPoint = reachedPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.reach_position;}
        }

        public override Position Position
        {
            get { return ReachedPoint.ToPosition(); }
        }
    }



    public class ReachPositionZoneTarget : ZoneMissionTarget<ReachPositionEventInfo>
    {
        public ReachPositionZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress) : base(zone, player, target, zoneMissionInProgress) { }

        protected override bool CanHandleMissionEvent(ReachPositionEventInfo e)
        {
            Log("checking position " + MyTarget.targetPosition + " VS " + e.ReachedPoint);

            if (!IsZoneOrPositionValid(e.ReachedPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnHandleMissionEvent(ReachPositionEventInfo e)
        {
            OnTargetComplete();
            this.SendReportToMissionEngine();  
            Log("position reached! " + MyTarget.targetPosition + " current: " + e.ReachedPoint);
            
        }
    }


    public class PopNpcEventInfo : MissionEventInfo
    {
        public Point PoppedAtPoint { get; private set; }

        public PopNpcEventInfo(Player player, Point poppedAtpoint) : base(player)
        {
            PoppedAtPoint = poppedAtpoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.pop_npc;}
        }

        public override Position Position
        {
            get { return PoppedAtPoint.ToPosition(); }
        }
    }


    public class PopNpcZoneTarget : ZoneMissionTarget<PopNpcEventInfo>
    {
        private readonly IPresenceManager _presenceManager;
        public PopNpcZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress,IPresenceManager presenceManager) : base(zone, player, target, zoneMissionInProgress)
        {
            _presenceManager = presenceManager;
        }


        protected override void OnHandleMissionEvent(PopNpcEventInfo e)
        {
            //save for later use
            OnTargetComplete();

            this.SendReportToMissionEngine();  

            Log("pop npc position reached! " + MyTarget.targetPosition + " current: " + e.PoppedAtPoint);
        }

        protected override bool CanHandleMissionEvent(PopNpcEventInfo e)
        {
            Log("pop npc checking position " + MyTarget.targetPosition + " VS " + e.PoppedAtPoint);

            if (!IsZoneOrPositionValid(e.PoppedAtPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();

            //pop npc spawns on the target center
            SpawnNpcOnSuccess(_presenceManager,MyTarget.targetPosition);
            
            if (MyTarget.ValidPresenceSet)
            {
                //handles static presence, technically spawns a presence if set
                
                // -------------------------------------
                // this is the oldschool way - legacy
                // -------------------------------------

                Task.Run(() =>
                {
                    try
                    {
                        // itt rakunk ki npcket
                        var presence = Zone.AddDynamicPresenceToPosition(MyTarget.NpcPresenceId, successEventInfo.Position);

                        foreach (var npc in presence.Flocks.GetMembers())
                        {
                            npc.Tag(Player, TimeSpan.FromHours(1));//mission presence-ben hosszu idore vannak taggelve
                            npc.AddDirectThreat(Player, 40 + FastRandom.NextDouble(0.0, 3.0));
                        }

                        Zone.CreateBeam(BeamType.teleport_storm,builder => builder.WithPosition(successEventInfo.Position).WithDuration(100000));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("error occured handling fix presence in " + this);
                        Logger.Exception(ex);
                    }
                });
            }

              
            
        }
    }

    public class LockUnitEventInfo : MissionEventInfo
    {
        public Npc LockedNpc { get; private set; }
        public Point LockedPosition { get; private set; }

        public LockUnitEventInfo(Player player, Npc lockedUnit, Point lockedPosition) : base(player)
        {
            LockedNpc = lockedUnit;
            LockedPosition = lockedPosition;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.lock_unit;}
        }

        public override Position Position
        {
            get { return LockedPosition.ToPosition(); }
        }
    }



    public class LockUnitZoneTarget : ZoneMissionTarget<LockUnitEventInfo>
    {
        private readonly HashSet<long> _lockedUnits = new HashSet<long>();
        private readonly ProgressCounter _progressCounter;

        public LockUnitZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override bool CanHandleMissionEvent(LockUnitEventInfo e)
        {
            var npc = e.LockedNpc;

            if (MyZoneMissionInProgress.missionGuid != npc.GetMissionGuid())
                return false;

            if (_lockedUnits.Contains(npc.Eid))
            {
                //unit already locked
               
                return false;
            }

            Log("marked npc was locked " + this);

            return true;
        }

        protected override void OnHandleMissionEvent(LockUnitEventInfo e)
        {
            _lockedUnits.Add(e.LockedNpc.Eid);

            _progressCounter.Current ++;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
            }

            this.SendReportToMissionEngine();
        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();

            //drops the scan document dummy item to use for delivery
            DropLootFromSecondaryDefinition(Zone, successEventInfo.Position);
            
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _progressCounter.AddToDictionary(info);
            info.Add("lockedUnits", _lockedUnits.ToArray());
            return info;
        }
    }

    public class KillEventInfo : MissionEventInfo
    {
        public Point KillPoint { get; private set; }
        public Npc KilledNpc { get; private set; }

        public KillEventInfo(Player player, Npc killedNpc, Point killPoint) : base(player)
        {
            KillPoint = killPoint;
            KilledNpc = killedNpc;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.kill_definition;}
        }

        public override Position Position
        {
            get { return KillPoint.ToPosition(); }
        }
    }


    public class KillZoneTarget : ZoneMissionTarget<KillEventInfo>
    {
        private readonly ProgressCounter _progressCounter;

        public KillZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target,  zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override void OnHandleMissionEvent(KillEventInfo missionEventInfo)
        {
            _progressCounter.Current ++;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
            }
            
            SendReportToMissionEngine();    
        }

        protected override bool CanHandleMissionEvent(KillEventInfo e)
        {
            var definitionKilled = e.KilledNpc.Definition;

            //quantity only mode, the new shit
            if (MyTarget.useQuantityOnly)
            {
                if (MyZoneMissionInProgress.missionGuid != e.KilledNpc.GetMissionGuid())
                    return false;

                Log("marked npc was killed " + this);
                return true;
            }

            //definition mode - oldschool
            if (MyTarget.Definition != definitionKilled)
                return false;

            if (!IsZoneOrPositionValid(e.KillPoint.ToPosition()))
                return false;

            Log("kill target progressed " + this);

            return true;


        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();

            //mission loot needed?
            DropLootFromSecondaryDefinition(Zone, successEventInfo.Position);

             
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _progressCounter.AddToDictionary(info);
            return info;
        }
    }

    public class ScanMaterialEventInfo : MissionEventInfo
    {
        public int ScannedDefinition { get; private set; }
        public MaterialProbeType ScanProbeType { get; private set; }
        public Point ScanPoint { get; private set; }

        public ScanMaterialEventInfo(Player player, int scannedDefinition, MaterialProbeType probeType, Point scanPoint) : base(player)
        {
            ScannedDefinition = scannedDefinition;
            ScanProbeType = probeType;
            ScanPoint = scanPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.scan_mineral;}
        }

        public override Position Position
        {
            get { return ScanPoint.ToPosition(); }
        }

        public override bool IsDefinitionMatching(IZoneMissionTarget missionTarget)
        {
            return missionTarget.MyTarget.Definition == ScannedDefinition;
        }
    }


    public class ScanMaterialZoneTarget : ZoneMissionTarget<ScanMaterialEventInfo>
    {
        public ScanMaterialZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress) : base(zone, player, target, zoneMissionInProgress) { }

        protected override void OnHandleMissionEvent(ScanMaterialEventInfo e)
        {
            OnTargetComplete();
            this.SendReportToMissionEngine();  
        }

        protected override bool CanHandleMissionEvent(ScanMaterialEventInfo e)
        {
            if (e.ScanProbeType != MyTarget.GetProbeType)
                return false;

            if (e.ScannedDefinition != MyTarget.MineralDefinition)
                return false;

            if (!IsZoneOrPositionValid(e.ScanPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();

            //drops the scan document dummy item to use for delivery
            DropLootFromSecondaryDefinition(Zone, successEventInfo.Position);

              
        }
    }

    public class ScanUnitEventInfo : MissionEventInfo
    {
        public Npc ScannedNpc { get; private set; }
        public Point ScannedPoint { get; private set; }

        public ScanUnitEventInfo(Player player, Npc scannedNpc, Point scannedPoint) : base(player)
        {
            ScannedNpc = scannedNpc;
            ScannedPoint = scannedPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.scan_unit;}
        }

        public override Position Position
        {
            get { return ScannedPoint.ToPosition(); }
        }
    }



    public class ScanUnitZoneTarget : ZoneMissionTarget<ScanUnitEventInfo>
    {
        private readonly HashSet<long> _scannedUnits = new HashSet<long>();
        private readonly ProgressCounter _progressCounter;

        public ScanUnitZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override bool CanHandleMissionEvent(ScanUnitEventInfo e)
        {
            if (_scannedUnits.Contains(e.ScannedNpc.Eid))
            {
                //unit already scanned
                Player.Character.CreateErrorMessage(Commands.MissionError,ErrorCodes.UnitAlreadyScanned).Send();
                return false;
            }

            if (MyTarget.Definition != e.ScannedNpc.Definition)
                return false;

            if (!IsZoneOrPositionValid(e.ScannedPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnHandleMissionEvent(ScanUnitEventInfo e)
        {
            _scannedUnits.Add(e.ScannedNpc.Eid);

            _progressCounter.Current++;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
            }
            
            this.SendReportToMissionEngine();
            
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _progressCounter.AddToDictionary(info);
            return info;
        }
    }

    public class ScanContainerEventInfo : MissionEventInfo
    {
        public Npc ScannedNpc { get; private set; }
        public Point ScanPoint { get; private set; }

        public ScanContainerEventInfo(Player player, Npc scannedNpc, Point scanPoint) : base(player)
        {
            ScannedNpc = scannedNpc;
            ScanPoint = scanPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.scan_container;}
        }

        public override Position Position
        {
            get { return ScanPoint.ToPosition(); }
        }
    }



    public class ScanContainerZoneTarget : ZoneMissionTarget<ScanContainerEventInfo>
    {
        private readonly HashSet<long> _scannedUnits = new HashSet<long>();
        private readonly ProgressCounter _progressCounter;

        public ScanContainerZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override bool CanHandleMissionEvent(ScanContainerEventInfo e)
        {
            if (_scannedUnits.Contains(e.ScannedNpc.Eid))
            {
                // unit already scanned
                Player.Character.CreateErrorMessage(Commands.MissionError,ErrorCodes.UnitAlreadyScanned).Send();
                return false;
            }

            if (MyTarget.Definition != e.ScannedNpc.Definition)
                return false;

            if (!IsZoneOrPositionValid(e.ScanPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnHandleMissionEvent(ScanContainerEventInfo e)
        {
            _scannedUnits.Add(e.ScannedNpc.Eid);

            _progressCounter.Current ++;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
            }

            this.SendReportToMissionEngine();    
            
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _progressCounter.AddToDictionary(info);
            return info;
        }
    }

    public class HarvestPlantEventInfo : MissionEventInfo
    {
        public int HarvestedDefinition { get;private  set; }
        public int HarvestedQuantity { get; private set; }
        public Point HarvestedPoint { get; private set; }

        public HarvestPlantEventInfo(Player player, int harvestedDefinition, int harvestedQuantity, Point harvestedPoint):base(player)
        {
            HarvestedDefinition = harvestedDefinition;
            HarvestedQuantity = harvestedQuantity;
            HarvestedPoint = harvestedPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.harvest_plant;}
        }

        public override Position Position
        {
            get { return HarvestedPoint.ToPosition(); }
        }

        public override bool IsDefinitionMatching(IZoneMissionTarget missionTarget)
        {
            return missionTarget.MyTarget.Definition == HarvestedDefinition;
        }
    }


    public class HarvestPlantZoneTarget : ZoneMissionTarget<HarvestPlantEventInfo>
    {
        private readonly ProgressCounter _progressCounter;
        private const int EVERY_N = 3;

        public HarvestPlantZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override void OnHandleMissionEvent(HarvestPlantEventInfo e)
        {
            _progressCounter.Current += e.HarvestedQuantity;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
                this.SendReportToMissionEngine();  
            }
            else
            {
                if (_progressCounter.IsEveryNTurn(EVERY_N))
                    this.SendReportToMissionEngine();
            }
        }

        protected override bool CanHandleMissionEvent(HarvestPlantEventInfo e)
        {
            if (MyTarget.Definition != e.HarvestedDefinition)
                return false;

            if (!IsZoneOrPositionValid(e.HarvestedPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();

            //drops the scan document dummy item to use for delivery
            DropLootFromSecondaryDefinition(Zone, successEventInfo.Position);
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _progressCounter.AddToDictionary(info);
            return info;
        }
    }

    public class DrillMineralEventInfo : MissionEventInfo
    {
        public int DrilledDefinition { get; private set; }
        public int DrilledQuantity { get; private set; }
        public Point DrillPoint { get; private set; }

        public DrillMineralEventInfo(Player player, int drilledDefinition, int drilledQuantity, Point drillPoint) : base(player)
        {
            DrilledDefinition = drilledDefinition;
            DrilledQuantity = drilledQuantity;
            DrillPoint = drillPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.drill_mineral;}
        }

        public override Position Position
        {
            get { return DrillPoint.ToPosition(); }
        }

        public override bool IsDefinitionMatching(IZoneMissionTarget missionTarget)
        {
            return missionTarget.MyTarget.Definition == DrilledDefinition;
        }
    }

    


    public class DrillMineralZoneTarget : ZoneMissionTarget<DrillMineralEventInfo>
    {
        private readonly ProgressCounter _progressCounter;
        private const int EVERY_N = 3;


        public DrillMineralZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target,  zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override bool CanHandleMissionEvent(DrillMineralEventInfo e)
        {
            if (MyTarget.Definition != e.DrilledDefinition)
                return false;

            if (!IsZoneOrPositionValid(e.DrillPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnHandleMissionEvent(DrillMineralEventInfo e)
        {
            _progressCounter.Current += e.DrilledQuantity ;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
                this.SendReportToMissionEngine();  
            }
            else
            {
                if (_progressCounter.IsEveryNTurn(EVERY_N))
                    this.SendReportToMissionEngine();
            }
        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();

            //drops the scan document dummy item to use for delivery
            DropLootFromSecondaryDefinition(Zone, successEventInfo.Position);
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            _progressCounter.AddToDictionary(dictionary);
            return dictionary;
        }
    }

    public class SubmitItemEventInfo : MissionEventInfo
    {
        public Item SubmittedItem { get; private set; }
        public MissionStructure SubmitMissionStructure { get; private set; }
        public Point SubmitPoint { get; private set; }
        
        public SubmitItemEventInfo(Player player, Item submittedItem, MissionStructure submitMissionStructure, Point submitPoint) : base(player)
        {
            SubmittedItem = submittedItem;
            SubmitMissionStructure = submitMissionStructure;
            SubmitPoint = submitPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.submit_item;}
        }

        public override Position Position
        {
            get { return SubmitPoint.ToPosition(); }
        }

        public override bool IsDefinitionMatching(IZoneMissionTarget missionTarget)
        {
            return missionTarget.MyTarget.Definition == SubmittedItem.Definition;
        }
    }



    public class SubmitItemZoneTarget : ZoneMissionTarget<SubmitItemEventInfo>
    {
        private readonly ProgressCounter _progressCounter;

        public SubmitItemZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override bool CanHandleMissionEvent(SubmitItemEventInfo e)
        {
            if (MyTarget.Definition != e.SubmittedItem.Definition)
                return false;

            if (!IsZoneOrPositionValid(e.SubmitPoint.ToPosition()))
                return false;

            if (MyTarget.ValidMissionStructureEidSet)
            {
                if (MyTarget.MissionStructureEid == e.SubmitMissionStructure.Eid)
                {
                    //this kiosk, yay
                    return true;
                }
            }

            return false;
        }

        protected override void OnHandleMissionEvent(SubmitItemEventInfo e)
        {
            _progressCounter.Current += e.SubmittedItem.Quantity;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();
            }

            this.SendReportToMissionEngine();    
            
        }

        [CanBeNull]
        public SubmitItemEventInfo CreateSubmitItemEventInfo(Player submitterPlayer, Kiosk kiosk, Item item)
        {
            if (MyTarget.Definition != item.Definition) return null;

            var amountNeeded = _progressCounter.MaxValue - _progressCounter.Current;
            var o = item.Unstack(amountNeeded);
            
            var missionEvent = new SubmitItemEventInfo(submitterPlayer, o, kiosk, kiosk.CurrentPosition);

            Entity.Repository.Delete(o);

            return missionEvent;
        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            _progressCounter.AddToDictionary(dictionary);
            return dictionary;
        }
    }

    public class SwitchEventInfo : MissionEventInfo
    {
        public MissionStructure SwitchMissionStructure { get; private set; }
        public Point SwitchPosition { get; private set; }

        public SwitchEventInfo(Player player, MissionStructure switchMissionStructure, Point switchPosition) : base(player)
        {
            SwitchMissionStructure = switchMissionStructure;
            SwitchPosition = switchPosition;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.use_switch;}
        }

        public override Position Position
        {
            get { return SwitchPosition.ToPosition(); }
        }
    }

    public class AlarmSwitchZoneTarget : ZoneMissionTarget<SwitchEventInfo>
    {
        public AlarmSwitchZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress) : base(zone, player, target, zoneMissionInProgress) { }

        protected override bool CanHandleMissionEvent(SwitchEventInfo e)
        {
            if (MyTarget.ValidMissionStructureEidSet)
            {
                if (e.SwitchMissionStructure.Eid == MyTarget.MissionStructureEid)
                    return true;
            }

            return false;
        }

        protected override void OnHandleMissionEvent(SwitchEventInfo e)
        {
            OnTargetComplete();
            this.SendReportToMissionEngine();  
        }
    }

    public class ItemSupplyEventInfo : MissionEventInfo
    {
        public Item SuppliedItem { get; private set; }
        public MissionStructure ItemSupplyStructure { get; private set; }
        public Point SupplyPoint { get; private set; }

        public ItemSupplyEventInfo(Player player, Item suppliedItem, MissionStructure itemSupplyStructure, Point supplyPoint) : base(player)
        {
            SuppliedItem = suppliedItem;
            ItemSupplyStructure = itemSupplyStructure;
            SupplyPoint = supplyPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.use_itemsupply;}
        }

        public override Position Position
        {
            get { return SupplyPoint.ToPosition(); }
        }

        public override bool IsDefinitionMatching(IZoneMissionTarget missionTarget)
        {
            return missionTarget.MyTarget.Definition == SuppliedItem.Definition;
        }
    }


    public class ItemSupplyZoneTarget : ZoneMissionTarget<ItemSupplyEventInfo>
    {
        private readonly ProgressCounter _progressCounter;

        public ItemSupplyZoneTarget(IZone zone, Player player, MissionTarget target,ZoneMissionInProgress zoneMissionInProgress, ProgressCounter progressCounter)
            : base(zone, player, target, zoneMissionInProgress)
        {
            _progressCounter = progressCounter;
        }

        protected override bool CanHandleMissionEvent(ItemSupplyEventInfo e)
        {
            if (MyTarget.ValidMissionStructureEidSet)
            {
                if (e.ItemSupplyStructure.Eid == MyTarget.MissionStructureEid)
                    return true;
            }

            return false;
        }

        protected override void OnHandleMissionEvent(ItemSupplyEventInfo e)
        {
            _progressCounter.Current += e.SuppliedItem.Quantity;

            if (_progressCounter.IsCompleted)
            {
                OnTargetComplete();

            }

            this.SendReportToMissionEngine();

        }

        protected override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            _progressCounter.AddToDictionary(dictionary);
            return dictionary;
        }

        public int GetCurrentProgress()
        {
            return _progressCounter.Current;
        }
    }

    public class FindArtifactEventInfo : MissionEventInfo
    {
        public ArtifactType FoundArtifactType { get; private set; }
        public Point ArtifactPoint { get; private set; }

        public FindArtifactEventInfo(Player player, ArtifactType foundArtifactType, Point artifactPoint) : base(player)
        {
            FoundArtifactType = foundArtifactType;
            ArtifactPoint = artifactPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.find_artifact;}
        }

        public override Position Position
        {
            get { return ArtifactPoint.ToPosition(); }
        }
    }


    public class FindArtifactZoneTarget : ZoneMissionTarget<FindArtifactEventInfo>
    {
        private readonly IPresenceManager _presenceManager;
        public FindArtifactZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress,IPresenceManager presenceManager) : base(zone, player, target, zoneMissionInProgress)
        {
            _presenceManager = presenceManager;
        }

        public ArtifactType GetArtifactType()
        {
            return MyTarget.TargetArtifactType;
        }


        protected override void OnHandleMissionEvent(FindArtifactEventInfo e)
        {
            OnTargetComplete();
            this.SendReportToMissionEngine();  
        }

        protected override bool CanHandleMissionEvent(FindArtifactEventInfo e)
        {
            if (!IsZoneOrPositionValid(e.ArtifactPoint.ToPosition()))
                return false;

            if (e.FoundArtifactType != MyTarget.TargetArtifactType)
                return false;

            return true;
        }
        
        public Position GetPosition()
        {
            if (!MyTarget.ValidPositionSet)
            {
                Logger.Error("no x,y set artifact GetPosition. " + MyTarget + " " + MyZoneMissionInProgress);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            return MyTarget.targetPosition;
        }

        public int GetRange()
        {
            if (!MyTarget.ValidRangeSet)
            {
                Logger.Error("no range set artifact GetRange " + MyTarget + " " + MyZoneMissionInProgress);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            return MyTarget.TargetPositionRange;
        }

        protected override void OnTargetComplete()
        {
            base.OnTargetComplete();
  
            //mission loot needed?
            DropLootFromSecondaryDefinition(Zone, successEventInfo.Position);

            if (MyTarget.FindArtifactSpawnsNpcs)
            {
                //find artifact spawns npc on the artifact's position
                SpawnNpcOnSuccess(_presenceManager,successEventInfo.Position);
            }
           
        }
    }

    public class SummonEggEventInfo : MissionEventInfo
    {
        public int SummonedEggDefinition { get; private set; }
        public Point SummonedPoint { get; private set; }

        public SummonEggEventInfo(Player player, int summonedEggDefinition, Point summonedPoint) : base(player)
        {
            SummonedEggDefinition = summonedEggDefinition;
            SummonedPoint = summonedPoint;
        }

        public override MissionTargetType MissionTargetType
        {
            get { return MissionTargetType.summon_npc_egg;}
        }

        public override Position Position
        {
            get { return SummonedPoint.ToPosition(); }
        }
    }

    public class SummonNpcEggZoneTarget : ZoneMissionTarget<SummonEggEventInfo>
    {
        public SummonNpcEggZoneTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress) : base(zone, player, target, zoneMissionInProgress) { }

        protected override bool CanHandleMissionEvent(SummonEggEventInfo e)
        {
            if (MyTarget.Definition != e.SummonedEggDefinition)
                return false;

            if (!IsZoneOrPositionValid(e.SummonedPoint.ToPosition()))
                return false;

            return true;
        }

        protected override void OnHandleMissionEvent(SummonEggEventInfo missionEventInfo)
        {
            OnTargetComplete();
            this.SendReportToMissionEngine();  
        }
    }
}
