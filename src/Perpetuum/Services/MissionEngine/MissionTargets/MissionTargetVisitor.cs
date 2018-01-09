using System.Diagnostics;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    public abstract class MissionTargetVisitor
    {
        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget(MissionTarget missionTarget) {}


        [DebuggerStepThrough]
        public virtual void Visit_MissionTargetRunsOnZone(MissionTargetRunsOnZone missionTargetRunsOnZone)
        {
            Visit_MissionTarget(missionTargetRunsOnZone);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTargetProduction(MissionTargetProduction missionTargetProduction)
        {
            Visit_MissionTarget(missionTargetProduction);
        }


        //exception helper thingy
        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_rnd_point(RandomPointMissionTarget randomPointMissionTarget)
        {
            Visit_MissionTarget(randomPointMissionTarget);
        }

        #region config_targets

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_fetch_item(FetchItemMissionTarget fetchItemMissionTarget)
        {
            Visit_MissionTarget(fetchItemMissionTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_loot_item(LootItemMissionTarget lootItemMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(lootItemMissionTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_reach_position(ReachPositionMissionTarget reachPositionMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(reachPositionMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_kill_definition(KillDefinitionMissionTarget killDefinitionMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(killDefinitionMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_scan_mineral(ScanMineralMissionTarget scanMineralMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(scanMineralMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_scan_unit(ScanUnitMissionTarget scanUnitMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(scanUnitMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_scan_container(ScanContainerMissionTarget scanContainerMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(scanContainerMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_drill_mineral(DrillMineralMissionTarget drillMineralMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(drillMineralMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_submit_item(SubmitItemMissionTarget submitItemMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(submitItemMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_use_switch(UseSwitchMissionTarget useSwitchMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(useSwitchMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_find_artifact(FindArtifactMissionTarget findArtifactMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(findArtifactMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_dock_in(DockInMissionTarget dockInMissionTarget)
        {
            Visit_MissionTarget(dockInMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_use_itemsupply(UseItemsupplyMissionTarget useItemsupplyMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(useItemsupplyMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_prototype(PrototypeMissionTarget prototypeMissionTarget)
        {
            Visit_MissionTargetProduction(prototypeMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_massproduce(MassproduceMissionTarget massproduceMissionTarget)
        {
            Visit_MissionTargetProduction(massproduceMissionTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_research(ResearchMissionTarget researchMissionTarget)
        {
            Visit_MissionTargetProduction(researchMissionTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_teleport(TeleportMissionTarget teleportMissionTarget)
        {
            Visit_MissionTarget(teleportMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_harvest_plant(HarvestPlantMissionTarget harvestPlantMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(harvestPlantMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_summon_npc_egg(SummonNpcEggMissionTarget summonNpcEggMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(summonNpcEggMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_pop_npc(PopNpcMissionTarget popNpcMissionTarget)
        {
            Visit_MissionTargetRunsOnZone(popNpcMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_spawn_item(SpawnItemMissionTarget spawnItemMissionTarget)
        {
            Visit_MissionTarget(spawnItemMissionTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_lock_unit(LockUnitMissionTarget lockUnitMissionTarget)
        {
            Visit_MissionTarget(lockUnitMissionTarget);
        }


        #endregion

        #region random_targets


        [DebuggerStepThrough]
        public virtual void Visit_RandomMissionTarget(RandomMissionTarget randomMissionTarget)
        {
            Visit_MissionTarget(randomMissionTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionStructureTarget(MissionStructureTarget missionStructureTarget)
        {
            Visit_RandomMissionTarget(missionStructureTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_pop_npc(PopNpcRandomTarget popNpcRandomTarget)
        {
            Visit_RandomMissionTarget(popNpcRandomTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_kill_definition(KillRandomTarget killRandomTarget)
        {
            Visit_RandomMissionTarget(killRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_loot_definition(LootRandomTarget lootRandomTarget)
        {
            Visit_RandomMissionTarget(lootRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_use_switch(UseSwitchRandomTarget useSwitchRandomTarget)
        {
            Visit_MissionStructureTarget(useSwitchRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_submit_item(SubmitItemRandomTarget submitItemRandomTarget)
        {
            Visit_MissionStructureTarget(submitItemRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_use_itemsupply(ItemSupplyRandomTarget itemSupplyRandomTarget)
        {
            Visit_MissionStructureTarget(itemSupplyRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_find_artifact(FindArtifactRandomTarget findArtifactRandomTarget)
        {
            Visit_RandomMissionTarget(findArtifactRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_scan_mineral(ScanMineralRandomTarget scanMineralRandomTarget)
        {
            Visit_RandomMissionTarget(scanMineralRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_drill_mineral(DrillMineralRandomTarget drillMineralRandomTarget)
        {
            Visit_RandomMissionTarget(drillMineralRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_harvest_plant(HarvestPlantRandomTarget harvestPlantRandomTarget)
        {
            Visit_RandomMissionTarget(harvestPlantRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_fetch_item(FetchItemRandomTarget fetchItemRandomTarget)
        {
            Visit_RandomMissionTarget(fetchItemRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_massproduce(MassproduceRandomTarget massproduceRandomTarget)
        {
            Visit_RandomMissionTarget(massproduceRandomTarget);
        }


        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_research(ResearchRandomTarget researchRandomTarget)
        {
            Visit_RandomMissionTarget(researchRandomTarget);
        }

        [DebuggerStepThrough]
        public virtual void Visit_MissionTarget_RND_spawn_item(SpawnItemRandomTarget spawnItemRandomTarget)
        {
            Visit_RandomMissionTarget(spawnItemRandomTarget);
        }

        public virtual void Visit_MissionTarget_RND_lock_unit(LockUnitRandomTarget lockUnitRandomTarget)
        {
            Visit_RandomMissionTarget(lockUnitRandomTarget);
        }

        #endregion
    }
}
