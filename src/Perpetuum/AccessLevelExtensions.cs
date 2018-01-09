
namespace Perpetuum
{
    public static class AccessLevelExtensions
    {
        public static bool IsAny(this AccessLevel sourceAccessLevel, AccessLevel accessLevel)
        {
            return (sourceAccessLevel & accessLevel) > 0;
        }

        public static bool IsAnyPrivilegeSet(this AccessLevel accessLevel)
        {
            const AccessLevel authMask = ~(AccessLevel.normal);
            return ((uint)accessLevel & (uint)authMask) > 0;
        }

        public static bool IsAdminOrGm(this AccessLevel accessLevel)
        {
            switch (accessLevel)
            {
                case AccessLevel.admin:
                case AccessLevel.gameAdmin:
                case AccessLevel.owner:
                    return true;
            }

            return false;
        }
    }
}
