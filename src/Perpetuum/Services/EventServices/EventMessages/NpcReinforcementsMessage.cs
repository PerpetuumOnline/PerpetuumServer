using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class NpcReinforcementsMessage : IEventMessage
    {
        public EventType Type => EventType.NpcReinforce;

        public Npc Npc { get; }
        public int ZoneId { get; }

        public NpcReinforcementsMessage(Npc npc, int zoneID)
        {
            ZoneId = zoneID;
            Npc = npc;
        }
    }
}
