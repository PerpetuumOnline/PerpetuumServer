using System;

namespace Perpetuum.Groups.Corporations
{
    [Flags,Serializable]
    public enum CorporationRole
    {
        NotDefined = 0, 
        CEO = 1,  
        HRManager = 1 << 1, // 2
        CorporationDelegate = 1 << 2, //4
        Accountant = 1 << 3, //8
        DeputyCEO = 1 << 4, // 16
        VoteAdmin = 1 << 5, // 32
        PRManager = 1 << 6,
        HangarAccess_low = 1 << 7,
        HangarAccess_medium = 1 << 8,
        HangarAccess_high = 1 << 9,
        HangarAccess_secure = 1 << 10,
        ProductionManager = 1 << 11,
        AllianceDelegate = 1 << 12,
        HangarOperator = 1 << 13,
        HangarRemove_low = 1 << 14,
        HangarRemove_medium = 1 << 15,
        HangarRemove_high = 1 << 16,
        HangarRemove_secure = 1 << 17,
        viewPBS = 1 << 18,
        editPBS = 1 << 19,
        TechTreeList = 1 << 20,
        TechTreeUnlock = 1 << 21,
    }
}