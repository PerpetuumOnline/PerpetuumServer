using Perpetuum.Groups.Gangs;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Zones
{
    public class TrainingZone : Zone
    {
        public TrainingZone(ISessionManager sessionManager, IGangManager gangManager) : base(sessionManager, gangManager)
        {
        }
    }
}