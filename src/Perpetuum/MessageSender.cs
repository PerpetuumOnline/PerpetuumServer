using System;
using System.Collections.Generic;
using System.Threading;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Services.Sessions;

namespace Perpetuum
{
    public class CorporationMessageSender : ICorporationMessageSender
    {
        private readonly IMessageSender _messageSender;
        private readonly Queue<MessageInfo> _messageQueue = new Queue<MessageInfo>();
        private bool _processing;

        public CorporationMessageSender(IMessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        private class MessageInfo
        {
            public IMessage message;
            public long corporationEid;
            public Corporation corporation;
            public CorporationRole role = CorporationRole.NotDefined;
        }

        public void SendToAll(IMessage message,Corporation corporation)
        {
            var mi = new MessageInfo
            {
                message = message,
                corporation = corporation
            };
            Enqueue(mi);
        }

        public void SendByCorporationRole(IMessage message,long corporationEid,CorporationRole role)
        {
            var mi = new MessageInfo
            {
                message = message,
                corporationEid = corporationEid,
                role = role
            };
            Enqueue(mi);
        }

        public void SendByCorporationRole(IMessage message,Corporation corporation,CorporationRole role)
        {
            var mi = new MessageInfo
            {
                message = message,
                corporation = corporation,
                role = role
            };
            Enqueue(mi);
        }

        private void Enqueue(MessageInfo messageInfo)
        {
            if (messageInfo.corporationEid == 0 && messageInfo.corporation == null)
                return;

            lock (_messageQueue)
            {
                if (_processing)
                {
                    _messageQueue.Enqueue(messageInfo);
                    return;
                }

                _processing = true;
                ThreadPool.UnsafeQueueUserWorkItem(_ => ProcessMessageQueue(messageInfo),null);
            }
        }

        private void ProcessMessageQueue(MessageInfo messageInfo)
        {
            var corporations = new Dictionary<long,Corporation>();

            while (true)
            {
                if (!corporations.TryGetValue(messageInfo.corporationEid,out Corporation corporation))
                {
                    if (messageInfo.corporation != null)
                    {
                        corporation = messageInfo.corporation;
                    }
                    else
                    {
                        corporation = Corporation.Get(messageInfo.corporationEid);

                        if (corporation != null)
                        {
                            corporations[messageInfo.corporationEid] = corporation;
                        }
                    }
                }

                if (corporation != null)
                {
                    var members = messageInfo.role != CorporationRole.NotDefined ? corporation.GetMembersByRole(messageInfo.role) : 
                                                                                   corporation.GetCharacterMembers();

                    _messageSender.SendToCharacters(messageInfo.message,members);
                }

                lock (_messageQueue)
                {
                    if (_messageQueue.Count == 0)
                    {
                        _processing = false;
                        return;
                    }

                    messageInfo = _messageQueue.Dequeue();
                }
            }
        }

    }

    public class MessageSender : IMessageSender
    {
        private readonly ISessionManager _sessionManager;

        public MessageSender(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public void SendToClient(IMessage message,ISession session)
        {
            session?.SendMessage(message);
        }

        public void SendToCharacter(IMessage message,Character character)
        {
            var session = _sessionManager.GetByCharacter(character);
            SendToClient(message,session);
        }

        public void SendToCharacters(IMessage message,IEnumerable<Character> characters)
        {
            foreach (var character in characters)
            {
                SendToCharacter(message,character);
            }
        }

        public void SendToAll(IMessage message)
        {
            foreach (var session in _sessionManager.Sessions)
            {
                SendToClient(message,session);
            }
        }

        public void SendToOnlineCharacters(IMessage message)
        {
            foreach (var character in _sessionManager.SelectedCharacters)
            {
                SendToCharacter(message,character);
            }
        }

        public void SendToAccount(IMessage message, Account account)
        {
            var session = _sessionManager.GetByAccount(account);
            SendToClient(message,session);
        }
    }
}