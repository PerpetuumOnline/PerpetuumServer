using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class NpcReinforcementsMessage : EventMessage
    {
        public Npc Npc { get; }
        public int ZoneId { get; }

        public NpcReinforcementsMessage(Npc npc, int zoneID)
        {
            ZoneId = zoneID;
            Npc = npc;
        }
    }
}
