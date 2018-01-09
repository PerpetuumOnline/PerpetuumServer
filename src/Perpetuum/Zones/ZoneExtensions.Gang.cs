using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Builders;
using Perpetuum.Groups.Gangs;
using Perpetuum.Players;

namespace Perpetuum.Zones
{
    public static partial class ZoneExtensions
    {
        public static IEnumerable<Player> GetGangMembersByGangId(this IZone zone, Guid id)
        {
            var players = new List<Player>();

            foreach (var player in zone.Players)
            {
                var gang = player.Gang;
                if ( gang == null )
                    continue;

                if (gang.Id == id)
                {
                    players.Add(player);
                }
            }

            return players;
        }

        public static IEnumerable<Player> GetGangMembers(this IZone zone, Gang gang)
        {
            return gang == null ? new Player[0] : zone.Players.Where(player => player.Gang == gang);
        }

        public static void SendPacketToGang(this IZone zone, Gang gang, IBuilder<Packet> packetBuidler, long exceptMemberEid = 0L)
        {
            var packet = packetBuidler.Build();
            zone.SendPacketToGang(gang, packet, exceptMemberEid);
        }

        public static void SendPacketToGang(this IZone zone, Gang gang, Packet packet, long exceptMemberEid = 0L)
        {
            zone.GetGangMembers(gang).Where(player => player.Eid != exceptMemberEid).ForEach(p =>
            {
                p.Session.SendPacket(packet);
            });
        }
    }
}
