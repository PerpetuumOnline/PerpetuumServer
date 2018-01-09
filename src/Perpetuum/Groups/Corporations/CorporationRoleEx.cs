using System.Linq;

namespace Perpetuum.Groups.Corporations
{
    public static class CorporationRoleEx
    {
        public static CorporationRole ClearRole(this CorporationRole currentRole, CorporationRole clearThis)
        {
            return (CorporationRole) ((uint) currentRole & ~((uint) clearThis));
        }
        
        public static CorporationRole SetRole(this CorporationRole currentRole, CorporationRole setThis)
        {
            return currentRole | setThis;
        }

        public static bool HasRole(this CorporationRole role, CorporationRole otherRole)
        {
            return (role & otherRole) > 0;
        }

        public static bool HasAllRoles(this CorporationRole role, params CorporationRole[] roles)
        {
           
            return roles.All(r => (role & r) != 0);
        }

        public static bool IsAnyRole(this CorporationRole role, params CorporationRole[] roles)
        {
            return roles.Any(r => (role & r) > 0);
        }

        public static CorporationRole GetHighestContainerAccess(this CorporationRole corporationRole)
        {
            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_secure))
            {
                return CorporationRole.HangarAccess_secure;
            }

            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_high))
            {
                return CorporationRole.HangarAccess_high;
            }

            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_medium))
            {
                return CorporationRole.HangarAccess_medium;
            }

            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_low))
            {
                return CorporationRole.HangarAccess_low;
            }

            return CorporationRole.HangarAccess_low;
        }

        public static CorporationRole GetRelatedRemoveAccess(this CorporationRole corporationRole)
        {
            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_secure))
            {
                return CorporationRole.HangarRemove_secure;
            }

            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_high))
            {
                return CorporationRole.HangarRemove_high;
            }

            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_medium))
            {
                return CorporationRole.HangarRemove_medium;
            }

            if (corporationRole.IsAnyRole(CorporationRole.HangarAccess_low))
            {
                return CorporationRole.HangarRemove_low;
            }

            return CorporationRole.HangarRemove_low;
        }

        public static CorporationRole CleanUpCharacterPBSRoles(this CorporationRole role)
        {
            if (role.HasFlag(CorporationRole.editPBS))
                role.SetRole(CorporationRole.viewPBS);

            return role;
        }

        public static CorporationRole CleanUpHangarAccess(this CorporationRole role)
        {
            var resultRole = (int)role;

            //has low remove => needs low access
            if (role.IsAnyRole(CorporationRole.HangarRemove_low))
                resultRole = (resultRole | (int) CorporationRole.HangarAccess_low);

            //has medium remove => needs medium access
            if (role.IsAnyRole(CorporationRole.HangarRemove_medium))
                resultRole = (resultRole | (int) CorporationRole.HangarAccess_medium);

            if (role.IsAnyRole(CorporationRole.HangarRemove_high))
                resultRole = (resultRole | (int) CorporationRole.HangarAccess_high);

            if (role.IsAnyRole(CorporationRole.HangarRemove_secure))
                resultRole = (resultRole | (int) CorporationRole.HangarAccess_secure);

            return (CorporationRole)resultRole;
        }
    }
}
