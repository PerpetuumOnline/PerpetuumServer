using System.Collections.Generic;
using Perpetuum.Threading.Process;

namespace Perpetuum.Groups.Gangs
{
    public interface IGangInviteService : IProcess
    {
        ICollection<GangInviteInfo> GetInvites();
        void AddInvite(GangInviteInfo inviteInfo);
        void RemoveInvite(GangInviteInfo inviteInfo);
    }
}