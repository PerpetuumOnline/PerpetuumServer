using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones.NpcSystem.Flocks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public class NpcStateAnnouncer : EventProcessor
    {
        private readonly IChannelManager _channelManager;
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";
        private const string CHANNEL = "Syndicate Radio";
        private readonly Character _announcer;
        private readonly IFlockConfigurationRepository _flockConfigReader;
        private readonly IDictionary<string, object> _nameDictionary;
        private readonly IDictionary<int, NpcStateMessage> _state;

        public NpcStateAnnouncer(IChannelManager channelManager, IFlockConfigurationRepository flockConfigurationRepo, ICustomDictionary customDictionary)
        {
            _announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
            _channelManager = channelManager;
            _flockConfigReader = flockConfigurationRepo;
            _nameDictionary = customDictionary.GetDictionary(0);
            _state = new Dictionary<int, NpcStateMessage>();
        }

        //TODO use ChannelMessageHandler w/ PreMadeChatMessages
        private readonly IList<string> _aliveMessages = new List<string>()
        {
            "has spawned!",
            "has appeared on Syndicate scanners",
            "has been detected"
        };

        private readonly IList<string> _deathMessages = new List<string>()
        {
            "has been defeated",
            "is no longer a threat to Syndicate activity",
            "'s signature is no longer detected at this time"
        };

        private int GetNpcDef(NpcStateMessage msg)
        {
            var config = _flockConfigReader.Get(msg.FlockId);
            return config?.EntityDefault?.Definition ?? -1;
        }

        private string GetNpcName(int def)
        {
            var nameToken = EntityDefault.Get(def).Name + "_name";
            var name = "";
            try
            {
                name = _nameDictionary[nameToken]?.ToString();
            } catch (KeyNotFoundException)
            {
                name = 'NameNotFound';
            }
            return name ?? string.Empty;
        }


        private string GetStateMessage(NpcStateMessage msg)
        {
            if (msg.State == NpcState.Alive)
            {
                return _aliveMessages[FastRandom.NextInt(_aliveMessages.Count - 1)];
            }
            else if (msg.State == NpcState.Dead)
            {
                return _deathMessages[FastRandom.NextInt(_deathMessages.Count - 1)];
            }
            return string.Empty;
        }

        private string BuildChatAnnouncement(NpcStateMessage msg)
        {
            var def = GetNpcDef(msg);
            if (def < 0)
                return string.Empty;

            var npcName = GetNpcName(def);
            if (npcName == string.Empty)
                return string.Empty;

            var stateMessage = GetStateMessage(msg);
            if (stateMessage == string.Empty)
                return string.Empty;

            return $"{npcName} {stateMessage}";
        }

        private static string Strip(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "");
        }

        private static string Abbreviate(string name, int charLim)
        {
            var words = name.Split(' ');
            var perWordLen = (charLim / (words.Length).Max(1)).Clamp(3, 24);
            for (var i = 0; i < words.Length; i++)
            {
                words[i] = Strip(words[i]).Clamp(perWordLen) ?? "";
            }
            return string.Join(" ", words);
        }

        private string BuildTopicFromState()
        {
            var topic = "Current: ";
            var allowableNameLens = (190 / (_state.Count).Max(1)).Clamp(3, 64);
            foreach (var pair in _state)
            {
                var name = Abbreviate(GetNpcName(pair.Key), allowableNameLens);
                if (name == string.Empty) continue;
                if(pair.Value.State == NpcState.Alive)
                    topic += $"{name}|";
            }
            return topic;
        }

        private bool IsUpdatable(NpcStateMessage current, NpcStateMessage next)
        {
            return next.TimeStamp > current.TimeStamp;
        }

        private bool UpdateState(NpcStateMessage msg)
        {
            var defKey = GetNpcDef(msg);
            if (defKey < 0)
            {
                return false;
            }
            else if (!_state.ContainsKey(defKey) || IsUpdatable(_state[defKey], msg))
            {
                _state[defKey] = msg;
                return true;
            }
            return false;
        }

        public override EventType Type => EventType.NpcState;
        public override void HandleMessage(IEventMessage value)
        {
            if (value is NpcStateMessage msg)
            {
                if (UpdateState(msg))
                {
                    var announcement = BuildChatAnnouncement(msg);
                    var motd = BuildTopicFromState();
                    if (!announcement.IsNullOrEmpty())
                    {
                        _channelManager.Announcement(CHANNEL, _announcer, announcement);
                    }
                    if (!motd.IsNullOrEmpty())
                    {
                        _channelManager.SetTopic(CHANNEL, _announcer, motd);
                    }
                }

            }
        }
    }
}
