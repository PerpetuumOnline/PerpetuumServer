using System;
using Perpetuum.Threading.Process;

namespace Perpetuum.Groups.Corporations
{
    public class VolunteerCEOProcessor : Process
    {
        private readonly Lazy<IVolunteerCEOService> _volunteerCEOLogic;

        public VolunteerCEOProcessor(Lazy<IVolunteerCEOService> volunteerCEOLogic)
        {
            _volunteerCEOLogic = volunteerCEOLogic;
        }

        public override void Update(TimeSpan time)
        {
            var expiredVolunteerCEOs = _volunteerCEOLogic.Value.GetExpiredVolunteers();

            foreach (var volunteerCeo in expiredVolunteerCEOs)
            {
                _volunteerCEOLogic.Value.TakeOverCeoRole(volunteerCeo);
            }
        }
    }
}