using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Perpetuum.Groups.Gangs
{
    public class GangInviteService : IGangInviteService
    {
        private ImmutableList<GangInviteInfo> _invites = ImmutableList<GangInviteInfo>.Empty;

        public GangInviteService(IGangManager gangManager)
        {
            gangManager.GangDisbanded += OnGangDisbanded;
        }

        private void OnGangDisbanded(Gang gang)
        {
            this.RemoveInvitesByGang(gang);
        }

        public ICollection<GangInviteInfo> GetInvites()
        {
            return _invites;
        }

        public void AddInvite(GangInviteInfo inviteInfo)
        {
            ImmutableInterlocked.Update(ref _invites,i => i.Add(inviteInfo));
        }

        public void RemoveInvite(GangInviteInfo inviteInfo)
        {
            ImmutableInterlocked.Update(ref _invites, i => i.Remove(inviteInfo));
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            var expired = new List<GangInviteInfo>();

            foreach (var gangInvite in _invites)
            {
                gangInvite.Update(time);

                if (!gangInvite.IsExpired)
                    continue;

                expired.Add(gangInvite);

                var data = new Dictionary<string, object>
                {
                    {k.characterID,gangInvite.member.Id},
                    {k.answer, -1}
                };

                Message.Builder.SetCommand(Commands.GangInviteReply).WithData(data).ToCharacters(gangInvite.sender).Send();
            }

            if (expired.Count > 0)
            {
                ImmutableInterlocked.Update(ref _invites, i => i.RemoveRange(expired));
            }
        }
    }
}