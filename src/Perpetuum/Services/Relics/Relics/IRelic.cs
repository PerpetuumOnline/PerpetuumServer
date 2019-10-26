using Perpetuum.Players;
using Perpetuum.Zones;
using System.Collections.Generic;

namespace Perpetuum.Services.Relics
{

    public interface IRelic 
    {
        void Init(RelicInfo info, IZone zone, Position position, RelicLootItems lootItems);
        void SetLoots(RelicLootItems lootItems);
        RelicInfo GetRelicInfo();
        Position GetPosition();
        void SetAlive(bool isAlive);
        bool IsAlive();
        void PopRelic(Player player);
        Dictionary<string, object> ToDebugDictionary();
        void RemoveFromZone();
    }

}


