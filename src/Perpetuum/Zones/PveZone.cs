using Perpetuum.Groups.Gangs;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Zones
{
    public class PveZone : Zone
    {
        public PveZone(ISessionManager sessionManager, IGangManager gangManager) : base(sessionManager, gangManager)
        {
        }
    }
}