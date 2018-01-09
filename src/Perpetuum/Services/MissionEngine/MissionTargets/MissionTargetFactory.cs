using System;
using System.Data;
using Perpetuum.Data;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    public static class MissionTargetFactory
    {
        public static MissionTarget GenerateMissionTargetFromConfigRecord(IDataRecord record)
        {
            var target = CreateTargetFromConfigRecord(record);
            target.PostLoadedAsConfigTarget();
            return target;
        }
        
        private static MissionTarget CreateTargetFromConfigRecord(IDataRecord record)
        {
            var targetType = (MissionTargetType)record.GetValue<int>(k.targetType.ToLower());

            switch (targetType)
            {
                case MissionTargetType.fetch_item:
                    return new FetchItemMissionTarget(record);
                case MissionTargetType.loot_item:
                    return new LootItemMissionTarget(record);
                case MissionTargetType.reach_position:
                    return new ReachPositionMissionTarget(record);
                case MissionTargetType.kill_definition:
                    return new KillDefinitionMissionTarget(record);
                case MissionTargetType.scan_mineral:
                    return new ScanMineralMissionTarget(record);
                case MissionTargetType.scan_unit:
                    return new ScanUnitMissionTarget(record);
                case MissionTargetType.scan_container:
                    return new ScanContainerMissionTarget(record);
                case MissionTargetType.drill_mineral:
                    return new DrillMineralMissionTarget(record);
                case MissionTargetType.submit_item:
                    return new SubmitItemMissionTarget(record);
                case MissionTargetType.use_switch:
                    return new UseSwitchMissionTarget(record);
                case MissionTargetType.find_artifact:
                    return new FindArtifactMissionTarget(record);
                case MissionTargetType.dock_in:
                    return new DockInMissionTarget(record);
                case MissionTargetType.use_itemsupply:
                    return new UseItemsupplyMissionTarget(record);
                case MissionTargetType.prototype:
                    return new PrototypeMissionTarget(record);
                case MissionTargetType.massproduce:
                    return new MassproduceMissionTarget(record);
                case MissionTargetType.research:
                    return new ResearchMissionTarget(record);
                case MissionTargetType.teleport:
                    return new TeleportMissionTarget(record);
                case MissionTargetType.harvest_plant:
                    return new HarvestPlantMissionTarget(record);
                case MissionTargetType.summon_npc_egg:
                    return new SummonNpcEggMissionTarget(record);
                case MissionTargetType.pop_npc:
                    return new PopNpcMissionTarget(record);
                case MissionTargetType.rnd_point:
                    return new RandomPointMissionTarget(record);
                case MissionTargetType.spawn_item:
                    return new SpawnItemMissionTarget(record);
                case MissionTargetType.lock_unit:
                    return new LockUnitMissionTarget(record);

                case MissionTargetType.rnd_pop_npc:
                    return new PopNpcRandomTarget(record);
                case MissionTargetType.rnd_kill_definition:
                    return new KillRandomTarget(record);
                case MissionTargetType.rnd_loot_definition:
                    return new LootRandomTarget(record);
                case MissionTargetType.rnd_use_switch:
                    return new UseSwitchRandomTarget(record);
                case MissionTargetType.rnd_submit_item:
                    return new SubmitItemRandomTarget(record);
                case MissionTargetType.rnd_use_itemsupply:
                    return new ItemSupplyRandomTarget(record);
                case MissionTargetType.rnd_find_artifact:
                    return new FindArtifactRandomTarget(record);
                case MissionTargetType.rnd_scan_mineral:
                    return new ScanMineralRandomTarget(record);
                case MissionTargetType.rnd_drill_mineral:
                    return new DrillMineralRandomTarget(record);
                case MissionTargetType.rnd_harvest_plant:
                    return new HarvestPlantRandomTarget(record);
                case MissionTargetType.rnd_fetch_item:
                    return new FetchItemRandomTarget(record);
                case MissionTargetType.rnd_massproduce:
                    return new MassproduceRandomTarget(record);
                case MissionTargetType.rnd_research:
                    return new ResearchRandomTarget(record);
                case MissionTargetType.rnd_spawn_item:
                    return new SpawnItemRandomTarget(record);
                case MissionTargetType.rnd_lock_unit:
                    return new LockUnitRandomTarget(record);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}
