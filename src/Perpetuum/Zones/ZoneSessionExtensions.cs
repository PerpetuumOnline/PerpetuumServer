using System.Collections.Generic;

namespace Perpetuum.Zones
{
    public static class ZoneSessionExtensions
    {

        public static void SendPackets(this IZoneSession session,IEnumerable<Packet> packets)
        {
            foreach (var packet in packets)
            {
                session.SendPacket(packet);
            }
        }
    }
}