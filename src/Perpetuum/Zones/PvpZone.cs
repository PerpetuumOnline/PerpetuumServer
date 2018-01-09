using Perpetuum.Groups.Gangs;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Zones
{
    public class PvpZone : Zone
    {
        public PvpZone(ISessionManager sessionManager, IGangManager gangManager) : base(sessionManager, gangManager)
        {
        }
    }
}