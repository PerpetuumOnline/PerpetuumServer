using System.Linq;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Sessions
{
    public static class SessionManagerExtensions
    {
        public static bool HasFreeSlot(this ISessionManager sessionManager)
        {
            var signedInClientsCount = sessionManager.Sessions.Count(c => c.IsAuthenticated);
            return sessionManager.MaxSessions > signedInClientsCount;
        }

        public static void DeselectCharacter(this ISessionManager sessionManager,Character character)
        {
            var session = sessionManager.GetByCharacter(character);
            session?.DeselectCharacter();
        }
    }
}