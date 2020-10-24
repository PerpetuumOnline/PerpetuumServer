using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;
using System.Linq;

namespace Perpetuum.Services.Channels.ChatCommands
{
    public class AdminCommandData
    {
        public class CommandArgs
        {
            public string Name { get; private set; }
            public string[] Args { get; private set; }
            public CommandArgs(string[] commandArray)
            {
                Name = commandArray[0].Substring(1).ToLower().Trim();
                Args = commandArray.Skip(1).ToArray();
            }
        }

        public static AdminCommandData Create(Character sender, string[] command, IRequest request, Channel channel, IChannelManager channelManager, ISessionManager sessionManager, bool devMode)
        {
            return new AdminCommandData
            {
                Sender = sender,
                Command = new CommandArgs(command),
                Request = request,
                Channel = channel,
                ChannelManager = channelManager,
                SessionManager = sessionManager,
                IsDevMode = devMode
            };
        }

        public Character Sender { get; private set; }
        public CommandArgs Command { get; private set; }
        public IRequest Request { get; private set; }
        public Channel Channel { get; private set; }
        public IChannelManager ChannelManager { get; private set; }
        public ISessionManager SessionManager { get; private set; }
        public bool IsDevMode { get; private set; }
    }
}
