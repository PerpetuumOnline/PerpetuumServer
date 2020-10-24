using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perpetuum.Services.Channels.ChatCommands
{
    public class AdminCommandRouter
    {
        private delegate void CommandDelegate(AdminCommandData data);

        private readonly GlobalConfiguration _config;
        private readonly ISessionManager _sessionManager;
        private readonly IDictionary<string, CommandDelegate> _commands;
        public AdminCommandRouter(GlobalConfiguration configuration, ISessionManager sessionManager)
        {
            _config = configuration;
            _sessionManager = sessionManager;

            _commands = typeof(AdminCommandHandlers).GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(ChatCommand), false).Length > 0)
                .Select(m => new KeyValuePair<string, CommandDelegate>(
                    ((ChatCommand)m.GetCustomAttribute(typeof(ChatCommand))).Command,
                    (CommandDelegate)Delegate.CreateDelegate(typeof(CommandDelegate), m)))
                .ToDictionary();
        }

        public void TryParseAdminCommand(Character sender, string text, IRequest request, Channel channel, IChannelManager channelManager)
        {
            if (IsAdminCommand(sender, text))
            {
                ParseAdminCommand(sender, text, request, channel, channelManager);
            }
        }

        private bool IsAdminCommand(Character sender, string message)
        {
            return message.StartsWith("#") && sender.AccessLevel == AccessLevel.admin;
        }

        private void ParseAdminCommand(Character sender, string text, IRequest request, Channel channel, IChannelManager channelManager)
        {
            if (!IsAdminCommand(sender, text))
                return;

            string[] command = text.Split(new char[] { ',' });

            var data = AdminCommandData.Create(sender, command, request, channel, channelManager, _sessionManager, _config.EnableDev);

            // Commands can only be issued in secure channel
            if (channel.Type == ChannelType.Admin)
            {
                TryInvokeCommand(data);
                return;
            }

            // Unless it is the command to secure the channel
            if (data.Command.Name == "secure")
            {
                AdminCommandHandlers.Secure(data);
                return;
            }
            channel.SendMessageToAll(_sessionManager, sender, "Channel must be secured before sending commands.");
        }

        private void TryInvokeCommand(AdminCommandData data)
        {
            if (_commands.TryGetValue(data.Command.Name, out CommandDelegate commandMethod))
            {
                commandMethod(data);
            }
            else
            {
                AdminCommandHandlers.Unknown(data);
            }
        }
    }
}
