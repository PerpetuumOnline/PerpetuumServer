using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Services.Channels
{
    public class ChannelManager : IChannelManager
    {
        private readonly ISessionManager _sessionManager;
        private readonly IChannelRepository _channelRepository;
        private readonly IChannelMemberRepository _memberRepository;
        private readonly IChannelBanRepository _banRepository;
        private readonly ChannelLoggerFactory _channelLoggerFactory;
        private readonly ConcurrentDictionary<string, Channel> _channels = new ConcurrentDictionary<string, Channel>();

        public ChannelManager(ISessionManager sessionManager,IChannelRepository channelRepository,IChannelMemberRepository memberRepository,IChannelBanRepository banRepository,ChannelLoggerFactory channelLoggerFactory)
        {
            _sessionManager = sessionManager;
            _sessionManager.SessionAdded += OnSessionAdded;

            _channelRepository = channelRepository;
            _memberRepository = memberRepository;
            _banRepository = banRepository;
            _channelLoggerFactory = channelLoggerFactory;

            foreach (var channel in channelRepository.GetAll())
            {
                _channels[channel.Name] = channel;
            }
        }

        private void OnSessionAdded(ISession session)
        {
            session.CharacterSelected += SessionOnCharacterSelected;
            session.CharacterDeselected += SessionOnCharacterDeselected;
        }

        private void SessionOnCharacterSelected(ISession session, Character character)
        {
            foreach (var kvp in _memberRepository.GetAllByCharacter(character))
            {
                var channelName = kvp.Key;
                var member = kvp.Value;

                var channel = UpdateChannel(channelName, c => c.SetMember(member));
                channel?.SendMemberOnlineStateToAll(_sessionManager, member, true);
            }
        }

        private void SessionOnCharacterDeselected(ISession session, Character character)
        {
            foreach (var name in _channels.Keys)
            {
                ChannelMember m = null;
                var channel = UpdateChannel(name, c =>
                {
                    m = c.GetMember(character);
                    return m == null ? c : c.RemoveMember(character);
                });

                channel?.SendMemberOnlineStateToAll(_sessionManager, m, false);
            }
        }

        public Channel GetChannelByName(string name)
        {
            return _channels.GetOrDefault(name);
        }

        public IEnumerable<Channel> Channels
        {
            get { return _channels.Values; }
        }

        public void CreateChannel(ChannelType type,string name)
        {
            var logger = _channelLoggerFactory(name);
            var channel = new Channel(type,name,logger);
            channel = _channelRepository.Insert(channel);
            _channels[name] = channel;
        }

        public void DeleteChannel(string channelName)
        {
            Channel channel;
            if ( !_channels.TryGetValue(channelName,out channel))
                return;

            _channelRepository.Delete(channel);
            _channels.Remove(channelName);

            foreach (var member in channel.Members)
            {
                var data = new Dictionary<string, object> { { k.member, member.ToDictionary() } };
                var n = channel.CreateNotificationMessage(ChannelNotify.RemoveMember, data);
                channel.SendToOne(_sessionManager, member.character, n);
            }
        }

        public void JoinChannel(string channelName, Character member, ChannelMemberRole role,string password)
        {
            ChannelMember newMember = null;
            var channel = UpdateChannel(channelName, c =>
            {
                if (c.IsOnline(member))
                    return c;

                if (_memberRepository.Get(c, member) != null)
                    return c;
                 
                if (member.AccessLevel.IsAdminOrGm())
                {
                    role |= PresetChannelRoles.ROLE_GOD;
                }
                else
                {
                    _banRepository.IsBanned(c, member).ThrowIfTrue(ErrorCodes.CharacterIsBanned);
                    c.CheckPasswordAndThrowIfMismatch(password);
                }

                newMember = new ChannelMember(member, role);
                _memberRepository.Insert(c, newMember);

                if (_sessionManager.IsOnline(member))
                    return c.SetMember(newMember);

                return c;
            });

            if (channel == null)
                return;

            channel.SendAddMemberToAll(_sessionManager,newMember);
            channel.SendJoinedToMember(_sessionManager,newMember);
            channel.Logger.MemberJoin(member);
        }

        public void LeaveAllChannels(Character character)
        {
            foreach (var channel in GetChannelsByMember(character))
            {
                LeaveChannel(channel.Name, character);
            }
        }

        public void LeaveChannel(string channelName, Character character)
        {
            UpdateChannel(channelName, c => LeaveChannel(c, character, false));
        }

        private Channel LeaveChannel(Channel channel, Character character, bool isKicked)
        {
            var m = _memberRepository.Get(channel, character);
            if (m == null)
                return channel;

            _memberRepository.Delete(channel, m);

            channel = channel.RemoveMember(m.character);

            var hasMembers = _memberRepository.HasMembers(channel);
            if (!hasMembers && !channel.IsConstant)
            {
                _channelRepository.Delete(channel);
                _banRepository.UnBanAll(channel);
                _channels.Remove(channel.Name);
            }

            if (!isKicked)
            {
                var data = new Dictionary<string, object> { { k.member, m.ToDictionary() } };
                var n = channel.CreateNotificationMessage(ChannelNotify.RemoveMember, data);

                channel.SendToAll(_sessionManager, n);
                channel.SendToOne(_sessionManager, character, n);
            }

            channel.Logger.MemberLeft(m.character);
            return channel;
        }

        public void SetPassword(string channelName, Character issuer, string password)
        {
            var channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_CHANGE_PASSWORD);
                c = c.SetPassword(password);
                _channelRepository.Update(c);
                return c;
            });

            if ( channel == null )
                return;

            var data = new Dictionary<string, object> { { k.issuerID, issuer.Id }, { k.password, password } };
            var n = channel.CreateNotificationMessage(ChannelNotify.ChangePassword, data);
            channel.SendToAll(_sessionManager, n);
        }

        public void SetTopic(string channelName, Character issuer, string topic)
        {
            var channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_CHANGE_TOPIC);
                c = c.SetTopic(topic);
                _channelRepository.Update(c);
                return c;
            });
            
            if ( channel == null )
                return;

            var data = new Dictionary<string, object> { { k.issuerID, issuer.Id }, { k.topic, topic } };
            var n = channel.CreateNotificationMessage(ChannelNotify.ChangeTopic, data);
            channel.SendToAll(_sessionManager, n);
            channel.Logger.TopicChanged(issuer, topic);
        }

        public void SetMemberRole(string channelName, Character issuer,Character character, ChannelMemberRole role)
        {
            // adminokra / gm-ekre nem lehet
            if (character.AccessLevel.IsAdminOrGm() && role == ChannelMemberRole.Undefined)
                return;

            ChannelMember m = null;
            var channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_MODIFY_MEMBER_ROLE);

                m = c.GetMember(character);
                if (m == null)
                    return c;

                m = m.WithRole(role);
                _memberRepository.Update(c, m);
                return c.SetMember(m);
            });
            
            if ( channel == null || m == null)
                return;

            var data = new Dictionary<string, object> { { k.issuerID, issuer.Id }, { k.member, m.ToDictionary() } };
            var n = channel.CreateNotificationMessage(ChannelNotify.ChangeMemberRole, data);
            channel.SendToAll(_sessionManager, n);
        }

        public void Talk(string channelName, Character sender, string message)
        {
            Channel channel;
            if ( !_channels.TryGetValue(channelName,out channel))
                return;

            var m = channel.GetMember(sender);
            if (m == null)
                return;

            channel.Logger.LogMessage(sender, message);

            m.CanTalk.ThrowIfFalse(ErrorCodes.CharacterIsMuted);
            channel.SendMessageToAll(_sessionManager,sender,message);
        }

        public void KickOrBan(string channelName, Character issuer, Character character, string message, bool ban)
        {
            if (issuer == character)
                return;

            // adminokat / gm-eket nem lehet kickelni
            character.AccessLevel.IsAdminOrGm().ThrowIfTrue(ErrorCodes.AccessDenied);

            ChannelMember m = null;

            var channel = UpdateChannel(channelName, c =>
            {
                c.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_KICK_MEMBER);

                if (ban)
                    _banRepository.Ban(c, character);

                m = c.GetMember(character);

                return LeaveChannel(c, character, true);
            });

            if (channel == null || m == null ) 
                return;

            var data = new Dictionary<string, object>
            {
                {k.issuerID, issuer.Id},
                {k.member, m.ToDictionary()},
                {k.ban,ban},
                {k.message,message}
            };

            var n = channel.CreateNotificationMessage(ChannelNotify.KickMember, data);
            channel.SendToAll(_sessionManager, n);
            channel.SendToOne(_sessionManager, character, n);
        }

        public void UnBan(string channelName, Character issuer, Character character)
        {
            Channel channel;
            if ( !_channels.TryGetValue(channelName,out channel) )
                return;

            channel.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_REMOVE_BAN);
            _banRepository.UnBan(channel, character);
        }

        [CanBeNull]
        private Channel UpdateChannel(string name, Func<Channel, Channel> channelUpdater)
        {
            var spinWait = new SpinWait();
            while (true)
            {
                Channel snapshot;
                if (!_channels.TryGetValue(name, out snapshot))
                    return null;

                var updated = channelUpdater(snapshot);
                if (updated == snapshot)
                    return snapshot;

                if (_channels.TryUpdate(name, updated, snapshot))
                    return updated;

                spinWait.SpinOnce();
            }
        }

        public IEnumerable<Character> GetBannedCharacters(string channelName, Character issuer)
        {
            Channel channel;
            if (!_channels.TryGetValue(channelName, out channel))
                return Enumerable.Empty<Character>();

            channel.CheckRoleAndThrowIfFailed(issuer, PresetChannelRoles.ROLE_CAN_LIST_BANNED_MEMBERS);
            return _banRepository.GetBannedCharacters(channel);
        }

        public IEnumerable<Channel> GetChannelsByMember(Character member)
        {
            return _channels.Values.Where(channel => channel.IsOnline(member));
        }

        public IEnumerable<Channel> GetPublicChannels()
        {
            return _channels.Values.Where(c => c.Type == ChannelType.Public || c.Type == ChannelType.Highlighted);
        }
    }
}