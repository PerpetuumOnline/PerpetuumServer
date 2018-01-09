using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Services.Channels;

namespace Perpetuum.Groups.Gangs
{
    public class GangManager : IGangManager
    {
        private readonly IGangRepository _gangRepository;
        private readonly IChannelManager _channelManager;
        private readonly Gang.Factory _gangFactory;
        private readonly ConcurrentDictionary<Guid,Gang> _gangs = new ConcurrentDictionary<Guid, Gang>();

        public GangManager(IGangRepository gangRepository,IChannelManager channelManager,Gang.Factory gangFactory)
        {
            _gangRepository = gangRepository;
            _channelManager = channelManager;
            _gangFactory = gangFactory;
        }

        public Gang GetGang(Guid gangID)
        {
            var gang = _gangs.GetOrAdd(gangID, _ => _gangRepository.Get(gangID));
            if (gang != null)
                return gang;

            _gangs.Remove(gangID);
            return null;
        }

        public Gang GetGangByMember(Character member)
        {
            var gang = _gangs.Values.FirstOrDefault(g => g.IsMember(member));
            if (gang != null)
                return gang;

            var gangID = _gangRepository.GetGangIDByMember(member);
            if (gangID == Guid.Empty)
                return null;

            gang = GetGang(gangID);
            return gang;
        }

        public Gang CreateGang(string gangName,Character leader)
        {
            if (string.IsNullOrEmpty(gangName))
                throw new PerpetuumException(ErrorCodes.GangNameTooShort);

            var gang = _gangFactory();
            gang.Id = Guid.NewGuid();
            gang.Name = gangName;
            gang.Leader = leader;
            gang.SetMember(leader);

            _gangRepository.Insert(gang);

            void Finish() => _channelManager.CreateAndJoinChannel(ChannelType.Gang, gang.ChannelName, gang.Leader);

            if (Transaction.Current != null)
                Transaction.Current.OnCommited(Finish);
            else
                Finish();

            return gang;
        }

        public void DisbandGang(Gang gang)
        {
            _gangRepository.Delete(gang);

            void Finish()
            {
                Message.Builder.SetCommand(Commands.GangDelete).WithData(gang.GetGangData()).ToCharacters(gang.GetMembers()).Send();
                _channelManager.DeleteChannel(gang.ChannelName);
                _gangs.Remove(gang.Id);
                GangDisbanded?.Invoke(gang);
            }

            if (Transaction.Current != null)
                Transaction.Current.OnCommited(Finish);
            else
                Finish();
        }

        public event Action<Gang> GangDisbanded;

        public void RemoveMember(Gang gang, Character member,bool isKick)
        {
            if (gang == null)
                return;

            if ( !gang.IsMember(member) )
                throw new PerpetuumException(ErrorCodes.CharacterNotInTheCurrentGang);

            _gangRepository.DeleteMember(gang,member);

            void Finish()
            {
                var data = new Dictionary<string, object>
                {
                    {k.data, gang.GetGangData()},
                    {k.memberID, member.Id}
                };

                var cmd = isKick ? Commands.GangKickMember : Commands.GangRemoveMember;
                Message.Builder.SetCommand(cmd).WithData(data).ToCharacters(gang.GetMembers()).Send();

                gang.RemoveMember(member);

                _channelManager.LeaveChannel(gang.ChannelName, member);

                OnGangMemberRemoved(gang, member);
            }

            if (Transaction.Current != null)
                Transaction.Current.OnCommited(Finish);
            else
                Finish();
        }

        protected virtual void OnGangMemberRemoved(Gang gang, Character member)
        {
            try
            {
                var members = gang.GetMembers().ToArray();

                if (members.Length <= 0)
                {
                    DisbandGang(gang);
                    return;
                }

                if (gang.Leader != member)
                    return;

                // nincs leader
                var newLeader = members.FirstOrDefault(mm => gang.HasRole(mm, GangRole.Assistant)) ?? Character.None;
                if (newLeader == Character.None)
                {
                    var firstMember = members.First();
                    newLeader = firstMember;
                }

                ChangeLeader(gang, newLeader);
            }
            finally
            {
                GangMemberRemoved?.Invoke(gang, member);
            }
        }


        public void ChangeLeader(Gang gang, Character newLeader)
        {
            if ( !gang.IsMember(newLeader))
                throw new PerpetuumException(ErrorCodes.CharacterNotInTheCurrentGang);

            _gangRepository.UpdateLeader(gang,newLeader);

            void Finish()
            {
                gang.Leader = newLeader;
                Message.Builder.SetCommand(Commands.GangSetLeader).WithData(new Dictionary<string, object>
                {
                    { k.leaderId, newLeader.Id }
                }).ToCharacters(gang.GetMembers()).Send();
                _channelManager.SetMemberRole(gang.ChannelName, newLeader, ChannelMemberRole.Operator);
                GangLeaderChanged?.Invoke(gang);
            }

            if (Transaction.Current != null)
                Transaction.Current.OnCommited(Finish);
            else
                Finish();
        }

        public event Action<Gang> GangLeaderChanged;

        public void JoinMember(Gang gang, Character member,bool joinChannel)
        {
            _gangRepository.InsertMember(gang,member);

            void Finish()
            {
                gang.SetMember(member);

                var data = new Dictionary<string, object>
                {
                    {k.data,gang.GetGangData()},
                    {k.memberID, member.Id}
                };

                Message.Builder.SetCommand(Commands.GangAddMember).WithData(data).ToCharacters(gang.GetMembers()).Send();

                GangMemberJoined?.Invoke(gang, member);

                if (joinChannel)
                {
                    _channelManager.JoinChannel(gang.ChannelName, member);
                }
            }

            if (Transaction.Current != null)
                Transaction.Current.OnCommited(Finish);
            else
                Finish();
        }

        public event Action<Gang,Character> GangMemberJoined;
        public event Action<Gang, Character> GangMemberRemoved;

        public void SetRole(Gang gang, Character member, GangRole newRole)
        {
            if (gang.Leader == member)
                return;

            if (!gang.IsMember(member))
                throw new PerpetuumException(ErrorCodes.CharacterNotInTheCurrentGang);

            _gangRepository.UpdateMemberRole(gang,member,newRole);

            void Finish()
            {
                gang.SetMember(member, newRole);
                Message.Builder.SetCommand(Commands.GangSetRole).WithData(new Dictionary<string, object>
                {
                    { k.memberID, member.Id },
                    { k.role, (int)newRole }
                }).ToCharacters(gang.GetMembers()).Send();

                var channelMemberRole = gang.HasRole(member, GangRole.Assistant) ? ChannelMemberRole.Operator : ChannelMemberRole.Undefined;
                _channelManager.SetMemberRole(gang.ChannelName, member, channelMemberRole);
            }

            if (Transaction.Current == null)
                Transaction.Current.OnCommited(Finish);
            else
                Finish();
        }

    }
}