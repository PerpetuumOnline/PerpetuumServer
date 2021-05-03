using Perpetuum.Accounting.Characters;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.Services.EventServices
{
    /// <summary>
    /// Simple EventProcessor example for demonstrating the EventListener system
    /// </summary>
    public class ChatEcho : EventProcessor
    {
        private readonly IChannelManager _channelManager;
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";
        private readonly Character _announcer;

        public ChatEcho(IChannelManager channelManager)
        {
            _announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            _channelManager = channelManager;
        }

        public override EventType Type => EventType.undefined;
        public override void HandleMessage(IEventMessage value)
        {
            if (value is EventMessageSimple msg)
            {
                _channelManager.Announcement("General chat", _announcer, msg.GetMessage());
            }

        }

    }

    /// <summary>
    /// NPC vicinity Chat Echo event handler
    /// Emits a message from an NPC on the Vicinity chat channel
    /// </summary>
    public class NpcChatEcho : EventProcessor
    {
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer"; //TODO "Nian" character
        private readonly Character _announcer;

        public NpcChatEcho()
        {
            _announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
        }

        public override EventType Type => EventType.NpcChat;
        public override void HandleMessage(IEventMessage value)
        {
            if (value is NpcMessage msg)
            {
                var src = msg.GetPlayerKiller();
                using (var chatPacket = new Packet(ZoneCommand.LocalChat))
                {
                    chatPacket.AppendInt(_announcer.Id);
                    chatPacket.AppendUtf8String(msg.GetMessage() + "\r\n");
                    src.SendPacketToWitnessPlayers(chatPacket, true);
                }
            }

        }
    }
}
