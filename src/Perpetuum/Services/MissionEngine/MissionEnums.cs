

namespace Perpetuum.Services.MissionEngine
{
    /// <summary>
    /// Controls the delivery messaging
    /// </summary>
    public enum DeliverResult
    {
        nothingHappened,
        partiallyDelivered,
        targetGroupAdvanced,
        completed,
        noDeliverableItemsFound
    }

    /// <summary>
    /// Target types for missions
    /// 
    /// 
    /// !! DO NOT RENAME THEM !!
    /// 
    /// 
    /// </summary>
    public enum MissionTargetType
    {
        fetch_item = 1,
        loot_item = 2,
        reach_position = 3 ,
        kill_definition =4,
        scan_mineral=5,
        scan_unit=6,
        scan_container=7,
        drill_mineral=8,
        submit_item=9,
        use_switch=10,
        find_artifact =11,
        dock_in =12,
        use_itemsupply = 13,
        prototype = 14, //NOT USED
        massproduce = 15, //needs ct and goes for the produced definition
        research = 16, // needs decoder and produces ct
        teleport = 17,
        harvest_plant = 18,
        summon_npc_egg = 19,
        pop_npc =20,
        rnd_point = 21,
        spawn_item = 22,
        lock_unit = 23,
        rnd_pop_npc = 101, //random targets above 100
        rnd_kill_definition, //102
        rnd_loot_definition, //103
        rnd_use_switch, //104
        rnd_submit_item, //105
        rnd_use_itemsupply, //106
        rnd_find_artifact, //107
        rnd_scan_mineral, //108
        rnd_drill_mineral, //109
        rnd_harvest_plant, //110
        rnd_fetch_item, //111
        
        
        rnd_massproduce, //112
        rnd_research, //113 
        rnd_spawn_item, //114
        rnd_lock_unit, //115
       
    }

    public enum MissionBehaviourType
    {
        Config = 1,
        Random = 2,
    }

    public enum MissionExtensionSelector
    {
        none,
        combat,
        fieldcraft,
        production,
        transport,
        
    }


    public enum MissionCategory
    {
        Industrial,
        Combat,
        Transport,
        Special,
        general_training,
        combat_training,
        industrial_training,
        Exploration,
        Harvesting,
        Mining,
        Production,
        CombatExploration,
        ComplexProduction,


    }

}
