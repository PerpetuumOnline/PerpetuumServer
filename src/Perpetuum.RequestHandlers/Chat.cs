using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers
{
    public class Chat : IRequestHandler
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILoggerCache _loggerCache;
        private readonly ChatLoggerFactory _chatLoggerFactory;

        public Chat(ISessionManager sessionManager,ILoggerCache loggerCache,ChatLoggerFactory chatLoggerFactory)
        {
            _sessionManager = sessionManager;
            _loggerCache = loggerCache;
            _chatLoggerFactory = chatLoggerFactory;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var target = Character.Get(request.Data.GetOrDefault<int>(k.target));
            var message = request.Data.GetOrDefault<string>(k.message);

            GetChatLogger(character, target).LogMessage(character, message);

            var data = new Dictionary<string, object>
            {
                { k.sender, character.Id }, 
                { k.target, target.Id }, 
                { k.message, message }
            };

            try
            {
                target.IsBlocked(character).ThrowIfTrue(ErrorCodes.TargetBlockedTheRequest);
                _sessionManager.GetByCharacter(target).ThrowIfNull(ErrorCodes.CharacterNotOnline);
                Message.Builder.SetCommand(request.Command).WithData(data).ToCharacter(target).Send();
            }
            catch (PerpetuumException gex)
            {
                data.Add(k.error, (int)gex.error);
            }

            Message.Builder.FromRequest(request).WithData(data).Send();
        }

        private ILogger<ChatLogEvent> GetChatLogger(Character sender, Character target)
        {
            var x = sender.Id;
            var y = target.Id;

            if (y > x)
            {
                ObjectHelper.Swap(ref x, ref y);
            }

            var hash = (ulong) x << 32 | (uint) y;

            var senderNick = sender.Nick;
                
            var logger = _loggerCache.GetOrAddLogger("private_" + hash, () =>
            {
                var filename = $"{senderNick} - {target.Nick}";
                return _chatLoggerFactory("private", filename);
            });

            return logger;
        }
    }
}