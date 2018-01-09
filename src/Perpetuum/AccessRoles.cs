using System;

namespace Perpetuum
{
    /// <summary>
    /// Roles to limit access to different ingame features/functions
    /// </summary>
    [Flags]
    public enum AccessRoles : uint
    {
        account = 1,
        terrainEditor = 1 << 1,
        levelEditor = 1 << 2,
        admin = 1 << 3,
        support = 1 << 4,
        host = 1 << 5,
        plugin = 1 << 6,
        developer = 1 << 7,
        storyTeller = 1 << 8,
        gameMaster = 1 << 9,
        notAuthenticated = 1 << 10,
        character = 1 << 11,
        channelOp = 1 << 12,
        websiteTester = 1 << 13,
        decorEditor = 1 << 14,
        npcEditor = 1 << 15,
        creditCheat = 1 << 16,
        robotTemplateEditor = 1 << 17,
        extensionOperator = 1 << 18,
        itemCheat = 1 << 19,
        productionCheater = 1 << 20,
        speedHacker = 1 << 21,
        privilegedTransactions = 1 << 22,
        trialAccount = 1 << 23,
        
    }
}