using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Corporations
{
    public interface IVolunteerCEOService
    {
        VolunteerCEO GetVolunteer(long corporationEid);
        VolunteerCEO AddVolunteer(Corporation corporation,Character character);
        void ClearVolunteer(VolunteerCEO volunteerCEO);
        IEnumerable<VolunteerCEO> GetExpiredVolunteers();
        void SendVolunteerStatusToMembers(VolunteerCEO volunteerCEO);
        void TakeOverCeoRole(VolunteerCEO volunteerCeo);
    }
}