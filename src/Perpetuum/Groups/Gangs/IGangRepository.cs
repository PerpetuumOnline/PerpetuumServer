using System;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Gangs
{
    public interface IGangRepository : IRepository<Guid,Gang>
    {
        void UpdateLeader(Gang gang, Character newLeader);

        void InsertMember(Gang gang, Character member);
        void DeleteMember(Gang gang, Character member);

        void UpdateMemberRole(Gang gang, Character member, GangRole newRole);

        Guid GetGangIDByMember(Character member);
    }
}