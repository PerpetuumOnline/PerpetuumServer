using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Services.Channels
{
    public class Channel
    {
        private Dictionary<Character,ChannelMember> _members = new Dictionary<Character, ChannelMember>();

        public int Id { get; private set; }
        public ChannelType Type { get; private set; }
        public string Name { get; private set; }
        public string Topic { get; private set; }
        public string Password { get; private set; }

        public IChannelLogger Logger { get; private set; }

        private Channel()
        {
            
        }

        public Channel(int id,ChannelType type,string name,string topic,string password,IChannelLogger logger) : this(type,name,logger)
        {
            Id = id;
            Topic = topic;
            Password = password;
        }

        public Channel(ChannelType type, string name,IChannelLogger logger)
        {
            Type = type;
            Name = name;

            Logger = logger;
        }

        public IEnumerable<ChannelMember> Members
        {
            get { return _members.Values; }
        }

        public Channel SetId(int id)
        {
            if (id == Id)
                return this;

            return new Channel
            {
                Id = id,
                Type = Type,
                Name = Name,
                Topic = Topic,
                Password = Password,
                Logger = Logger,
                _members = new Dictionary<Character, ChannelMember>(_members)
            };
        }

        public Channel SetTopic(string topic)
        {
            if ( !string.IsNullOrEmpty(topic) && topic.Length > 200)
                topic = topic.Substring(0, 199);

            if (topic == Topic)
                return this;

            return new Channel
            {
                Id = Id,
                Type = Type,
                Name = Name,
                Topic = topic,
                Password = Password,
                Logger = Logger,
                _members = new Dictionary<Character, ChannelMember>(_members)
            };
        }

        public Channel SetPassword(string password)
        {
            if (password == Password)
                return this;

            return new Channel
            {
                Id = Id,
                Type = Type,
                Name = Name,
                Topic = Topic,
                Password = password,
                Logger = Logger,
                _members = new Dictionary<Character, ChannelMember>(_members)
            };
        }

        public Channel SetMember(ChannelMember member)
        {
            if (member == null)
                return this;

            var members = new Dictionary<Character, ChannelMember>(_members) {[member.character] = member};

            return new Channel
            {
                Id = Id,
                Type = Type,
                Name = Name,
                Topic = Topic,
                Password = Password,
                Logger = Logger,
                _members = members
            };
        }

        public Channel RemoveMember(Character member)
        {
            var members = new Dictionary<Character, ChannelMember>(_members);
            if (!members.Remove(member))
                return this;

            return new Channel
            {
                Id = Id,
                Type = Type,
                Name = Name,
                Topic = Topic,
                Password = Password,
                Logger = Logger,
                _members = members
            };
        }

        [CanBeNull]
        public ChannelMember GetMember(Character member)
        {
            ChannelMember channelMember;
            return !_members.TryGetValue(member, out channelMember) ? null : channelMember;
        }

        public void CheckPasswordAndThrowIfMismatch(string password)
        {
            if (!HasPassword)
                return;

            Equals(Password, password ?? string.Empty).ThrowIfFalse(ErrorCodes.PasswordMismatch,gex => gex.SetData("channel",Name));
        }

        public void CheckRoleAndThrowIfFailed(Character member, ChannelMemberRole role)
        {
            if (member == Character.None)
                return;

            GetMember(member).ThrowIfNull(ErrorCodes.NotMemberOfChannel).HasRole(role).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
        }

        public bool IsOnline(Character character)
        {
            return GetMember(character) != null;
        }

        public IDictionary<string, object> ToDictionary(Character issuer, bool withMembers)
        {
            var isMember = true;
            if (issuer != Character.None)
                isMember = IsOnline(issuer);

            var hasPassword = HasPassword;

            var result = new Dictionary<string, object>
                             {
                                 {k.name,Name}, 
                                 {k.type, (int)Type},
                                 {k.password,hasPassword},
                             };

            if (isMember || !hasPassword)
            {
                result.Add(k.count, _members.Values.Count);
                result.Add(k.topic, Topic);
            }

            if (withMembers)
                result.Add(k.members, _members.Values.ToDictionary("m", m => m.ToDictionary()));

            return result;
        }

        private bool HasPassword
        {
            get { return !string.IsNullOrEmpty(Password); }
        }

        public bool IsConstant
        {
            get
            {
                switch (Type)
                {
                    case ChannelType.Public:
                        return false;
                }

                return true;
            }
        }

        public MessageBuilder CreateNotificationMessage(ChannelNotify notify, IDictionary<string, object> data)
        {
            var dictionary = new Dictionary<string, object>
            {
                {k.channel,Name},
                {k.command, (int)notify},
                {k.data,data}
            };

            return Message.Builder.SetCommand(Commands.ChannelNotification).WithData(dictionary);
        }

        public void SendToAll(ISessionManager sessionManager, MessageBuilder messageBuilder)
        {
            SendToAll(sessionManager, messageBuilder, Character.None);
        }

        public void SendToAll(ISessionManager sessionManager,MessageBuilder messageBuilder, Character sender)
        {
            if ( sessionManager == null || messageBuilder == null )
                return;

            var message = messageBuilder.Build();

            foreach (var member in _members.Keys)
            {
                if (sender != Character.None)
                {
                    if (member.IsBlocked(sender))
                        continue;
                }

                var session = sessionManager.GetByCharacter(member);
                session?.SendMessage(message);
            }
        }

        public void SendToOne(ISessionManager sessionManager,Character character, MessageBuilder messageBuilder)
        {
            var session = sessionManager?.GetByCharacter(character);
            session?.SendMessage(messageBuilder);
        }

        public void SendMessageToAll(ISessionManager sessionManager,Character sender,string message)
        {
            var data = new Dictionary<string, object>
            {
                { k.sender, sender.Id }, 
                { k.message, message }
            };

            var n = CreateNotificationMessage(ChannelNotify.Message, data);
            SendToAll(sessionManager, n, sender);
        }

        public void SendMemberOnlineStateToAll(ISessionManager sessionManager,ChannelMember member,bool isOnline)
        {
            if ( member == null )
                return;

            var data = new Dictionary<string, object> { { k.member, member.ToDictionary() }, { k.state, isOnline } };
            var n = CreateNotificationMessage(ChannelNotify.OnlineState, data);
            SendToAll(sessionManager, n);
        }

        public void SendAddMemberToAll(ISessionManager sessionManager,ChannelMember member)
        {
            if ( member == null || !sessionManager.IsOnline(member.character))
                return;

            var n = CreateNotificationMessage(ChannelNotify.AddMember, member.ToDictionary());
            SendToAll(sessionManager, n);
        }

        public void SendJoinedToMember(ISessionManager sessionManager,ChannelMember member)
        {
            if ( member == null )
                return;

            var d = new Dictionary<string, object>
                {
                    {k.member, member.ToDictionary()}, 
                    {k.channel,ToDictionary(member.character, true)}
                };

            var n = CreateNotificationMessage(ChannelNotify.Joined, d);
            SendToOne(sessionManager, member.character, n);
        }
    }
}