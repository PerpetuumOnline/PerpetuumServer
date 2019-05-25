using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using System.Linq;

namespace Perpetuum.Services.Channels
{
    public static class ChannelMessageHandler
    {
        // TODO hardcoded character and channel names
        private const string WELCOME_CHANNEL_NAME = "Recruitment";
        private const string HELP_CHANNEL_NAME = "regchannel_help";
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";

        public static void SendNewPlayerTutorialMessage(IChannelManager channelManager, string newCharacterName)
        {
            var test = channelManager.Channels.All(channel => channel.Name.ToLower().Contains("help"));

            SendMessage(channelManager, HELP_CHANNEL_NAME, PreMadeChatMessageNames.TUTORIAL_ARRIVE, newCharacterName);
        }

        public static void SendWelcomeMessageExitTutorial(IChannelManager channelManager, string newCharacterName)
        {
            SendMessage(channelManager, WELCOME_CHANNEL_NAME, PreMadeChatMessageNames.TUTORIAL_FINISH, newCharacterName);
        }

        private static void SendMessage(IChannelManager channelManager, string channelName, string messageName, string newCharacterName)
        {
            string message;
            using (var scope = Db.CreateTransaction())
            {
                var records = Db.Query()
                    .CommandText("SELECT message FROM premadechatmessage WHERE name = @messageName")
                    .SetParameter("@messageName", messageName)
                    .Execute();
                message = records.First().GetValue<string>("message");
                scope.Complete();
            }
            message = message.Replace("$NAME$", newCharacterName);

            Character sender = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            channelManager.Announcement(channelName, sender, message);
        }
    }
}
