using System;
using System.Collections;
using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;

namespace Perpetuum
{
    public class MessageBuilder
    {
        private readonly GlobalConfiguration _configuration;
        private Command _command;
        private object _data;
        private bool _wrapToResult;
        private bool _withEmpty;

        public delegate MessageBuilder Factory();

        public MessageBuilder()
        {
            
        }

        private readonly IMessageSender _messageSender;
        private readonly ICorporationMessageSender _corporationMessageSender;

        public MessageBuilder(GlobalConfiguration configuration,IMessageSender messageSender,ICorporationMessageSender corporationMessageSender)
        {
            _configuration = configuration;
            _messageSender = messageSender;
            _corporationMessageSender = corporationMessageSender;
        }

        public MessageBuilder SetCommand(Command command)
        {
            _command = command;
            return this;
        }

        private static readonly Dictionary<string, object> _emptyDictionary = new Dictionary<string, object>(1) { { k.state, k.empty } };

        public MessageBuilder WrapToResult()
        {
            _wrapToResult = true;
            return this;
        }

        public MessageBuilder WithEmpty()
        {
            _withEmpty = true;
            return this;
        }

        public MessageBuilder SetData<T>(string key, T value)
        {
            var dictionary = _data as IDictionary<string, object>;
            if (dictionary == null)
                _data = dictionary = new Dictionary<string, object>();

            dictionary[key] = value;
            return this;
        }

        public MessageBuilder WithData<T>(T data)
        {
            _data = data;
            return this;
        }

        private Action<IMessage> _sender;

        private MessageBuilder SetupMessageSender(Action<IMessage> messageSender)
        {
            _sender = messageSender;
            return this;
        }

        public MessageBuilder ToAll()
        {
            return SetupMessageSender(m => _messageSender.SendToAll(m));
        }

        public MessageBuilder ToOnlineCharacters()
        {
            return SetupMessageSender(m => _messageSender.SendToOnlineCharacters(m));
        }

        public MessageBuilder FromRequest(IRequest request)
        {
            return SetCommand(request.Command).ToClient(request.Session);
        }

        public MessageBuilder WithOk()
        {
            return SetData(k.result, k.oke);
        }

        public MessageBuilder WithException(Exception ex)
        {
            if (ex is PerpetuumException gex)
                return WithError(gex.error).SetData(k.extra,gex.Data.ToDictionary());

            return WithError(ErrorCodes.ServerError);
        }

        public MessageBuilder WithError(ErrorCodes error)
        {
            return SetData(k.rErr, (int)error);
        }

        public MessageBuilder SetExtraInfo(Action<Dictionary<string, object>> extraInfoBuilder)
        {
            var extra = new Dictionary<string, object>();
            extraInfoBuilder(extra);
            return SetData(k.extra, extra);
        }

        public IMessage Build()
        {
            var message = new Message(_command, BuildData())
            {
                Sender = _configuration?.RelayName
            };
            return message;
        }

        private Dictionary<string, object> BuildData()
        {
            if (_withEmpty)
            {
                var isNullOrEmpty = _data == null || (_data as IEnumerable).IsNullOrEmpty();

                if (isNullOrEmpty)
                    return _emptyDictionary;
            }

            if (_wrapToResult)
                return new Dictionary<string, object> { { k.result, _data } };

            if (_data is Dictionary<string, object> dictionary)
                return dictionary;

            if (_data is IEnumerable<KeyValuePair<string, object>> kvp)
            {
                return kvp.ToDictionary();
            }

            return null;
        }

        public void Send()
        {
            _sender?.Invoke(Build());
        }

        public MessageBuilder ToCorporation(Corporation corporation)
        {
            return SetupMessageSender(m => _corporationMessageSender.SendToAll(m,corporation));
        }

        public MessageBuilder ToCorporation(long corporationEid,CorporationRole role)
        {
            return SetupMessageSender(m => _corporationMessageSender.SendByCorporationRole(m,corporationEid,role));
        }

        public MessageBuilder ToCorporation(Corporation corporation,CorporationRole role)
        {
            return SetupMessageSender(m => _corporationMessageSender.SendByCorporationRole(m,corporation,role));
        }

        public MessageBuilder ToAccount(Account account)
        {
            return SetupMessageSender(m => _messageSender.SendToAccount(m,account));
        }

        public MessageBuilder ToCharacter(Character character)
        {
            return SetupMessageSender(m => _messageSender.SendToCharacter(m,character));
        }

        public MessageBuilder ToCharacters(params Character[] characters)
        {
            return ToCharacters((IEnumerable<Character>)characters);
        }

        public MessageBuilder ToCharacters(IEnumerable<Character> characters)
        {
            return SetupMessageSender(m => _messageSender.SendToCharacters(m,characters));
        }

        public MessageBuilder ToClient(ISession session)
        {
            return SetupMessageSender(m => _messageSender.SendToClient(m,session));
        }
    }
}