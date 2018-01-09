using System.Linq;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Gangs
{
    public static class GangInviteServiceExtensions
    {
        [CanBeNull]
        public static GangInviteInfo GetInvite(this IGangInviteService inviteService, Character character)
        {
            return inviteService.GetInvites().FirstOrDefault(i => i.member == character);
        }

        public static void RemoveInvitesByGang(this IGangInviteService inviteService, Gang gang)
        {
            foreach (var invite in inviteService.GetInvites())
            {
                if (invite.gangGuid == gang.Id)
                    invite.ForceRemove();
            }
        }
    }
}