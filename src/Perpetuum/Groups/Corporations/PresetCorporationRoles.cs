namespace Perpetuum.Groups.Corporations
{
    public static class PresetCorporationRoles
    {
        public const CorporationRole LEADER = CorporationRole.CEO | CorporationRole.DeputyCEO;
        public const CorporationRole CAN_UNLOCK_TECHTREE = LEADER | CorporationRole.TechTreeUnlock;
        public const CorporationRole CAN_LIST_TECHTREE = CAN_UNLOCK_TECHTREE | CorporationRole.TechTreeList;
        public const CorporationRole HANGAR_ACCESS_MASK = CorporationRole.HangarAccess_low | CorporationRole.HangarAccess_medium | CorporationRole.HangarAccess_high | CorporationRole.HangarAccess_secure;
        public const CorporationRole CAN_USE_GATE = LEADER;
    }
}