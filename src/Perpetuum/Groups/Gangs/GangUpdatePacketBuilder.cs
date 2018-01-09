using System.Collections.Generic;
using System.Linq;
using Perpetuum.Builders;
using Perpetuum.Players;
using Perpetuum.Zones;

namespace Perpetuum.Groups.Gangs
{
    public class GangUpdatePacketBuilder : IBuilder<Packet>
    {
        private readonly Visibility _visibility;
        private readonly Player[] _members;

        public GangUpdatePacketBuilder(Visibility visibility, Player member)
        {
            _visibility = visibility;
            _members = new[] { member };
        }

        public GangUpdatePacketBuilder(Visibility visibility, IEnumerable<Player> members)
        {
            _visibility = visibility;
            _members = members.ToArray();
        }

        public Packet Build()
        {
            var packet = new Packet(ZoneCommand.GangUpdate);
            packet.AppendByte((byte)_members.Length);

            foreach (var member in _members)
            {
                packet.AppendInt(member.Character.Id);
                packet.AppendInt(member.Definition);

                byte v = 0;
                if (_visibility == Visibility.Visible)
                    v = 1;
                
                packet.AppendByte(v);

                var pos = member.CurrentPosition;
                packet.AppendInt(pos.intX);
                packet.AppendInt(pos.intY);

                packet.AppendDouble(member.ArmorMax);
                packet.AppendDouble(member.Armor);
                packet.AppendDouble(member.CoreMax);
                packet.AppendDouble(member.Core);
            }

            return packet;
        }
    }
}