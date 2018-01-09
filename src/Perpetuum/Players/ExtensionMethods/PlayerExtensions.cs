using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.Players.ExtensionMethods
{
    public static class PlayerExtensions
    {
        public static void SendStartProgressBar(this IEnumerable<Player> players, Unit unit,TimeSpan timeout)
        {
            var data = unit.BaseInfoToDictionary();

            data.Add(k.timeOut, (int)timeout.TotalMilliseconds);
            data.Add(k.started, (long)GlobalTimer.Elapsed.TotalMilliseconds);
            data.Add(k.now, (long)GlobalTimer.Elapsed.TotalMilliseconds);

            Message.Builder.SetCommand(Commands.AlarmStart).WithData(data).ToCharacters(players.ToCharacters()).Send();
        }

        public static void SendEndProgressBar(this IEnumerable<Player> players, Unit unit,bool success = true)
        {
            var info = unit.BaseInfoToDictionary();
            info.Add(k.success, success);
            Message.Builder.SetCommand(Commands.AlarmOver).WithData(info).ToCharacters(players.ToCharacters()).Send();
        }

        public static IEnumerable<Character> ToCharacters(this IEnumerable<Player> players)
        {
            return players.Select(p => p.Character).ToArray();
        }

        public static void SendPacket(this IEnumerable<Player> players, IBuilder<Packet> packetBuilder)
        {
            var packet = packetBuilder.Build();
            players.SendPacket(packet);
        }

        public static void SendPacket(this IEnumerable<Player> players, Packet packet)
        {
            foreach (var player in players)
            {
                player.Session.SendPacket(packet);
            }
        }
    }
}
