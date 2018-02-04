using Perpetuum.Groups.Gangs;
using Perpetuum.Services.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Zones
{
    public class StrongHoldZone : Zone
    {
        public StrongHoldZone(ISessionManager sessionManager, IGangManager gangManager) : base(sessionManager, gangManager)
        {
        }
    }
}
