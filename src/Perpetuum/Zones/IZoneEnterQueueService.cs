using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Players;

namespace Perpetuum.Zones
{
    public interface IZoneEnterQueueService
    {
        void SendReplyCommand(Character character, Player player, Command replyCommand);
        void EnqueuePlayer(Character character, Command replyCommand);
        void LoadPlayerAndSendReply(Character character, Command replyCommand);
        void RemovePlayer(Character character);

        int MaxPlayersOnZone { get; set; }
        Dictionary<string, object> GetQueueInfoDictionary();
    }
}