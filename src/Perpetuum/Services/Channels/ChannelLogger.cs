using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers;
using Perpetuum.Log;

namespace Perpetuum.Services.Channels
{
    public interface IChannelLogger : ILogger<ChatLogEvent>
    {
        void TopicChanged(Character member, string topic);
        void MemberJoin(Character member);
        void MemberLeft(Character member);
    }

    public delegate IChannelLogger ChannelLoggerFactory(string name);

    public class ChannelLogger : IChannelLogger
    {
        private readonly ILogger<ChatLogEvent> _logger;

        public ChannelLogger(ILogger<ChatLogEvent> logger)
        {
            _logger = logger;
        }

        public void TopicChanged(Character member, string topic)
        {
            if (member == Character.None)
                return;

            _logger.LogMessage(member, $" has changed the topic to: {topic}");
        }

        public void MemberJoin(Character member)
        {
            if ( member == Character.None )
                return;

            _logger.LogMessage(member," has joined the channel.");
        }

        public void MemberLeft(Character member)
        {
            if (member == Character.None)
                return;

            _logger.LogMessage(member, " has left the channel.");
        }

        public void Log(ChatLogEvent logEvent)
        {
            _logger.Log(logEvent);
        }
    }
}