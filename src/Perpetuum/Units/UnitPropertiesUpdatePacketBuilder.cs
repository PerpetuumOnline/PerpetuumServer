using System.Collections.Generic;
using Perpetuum.Builders;
using Perpetuum.Items;
using Perpetuum.Zones;

namespace Perpetuum.Units
{
    public class UnitPropertiesUpdatePacketBuilder : IBuilder<Packet>
    {
        private readonly IEnumerable<ItemProperty> _properties;
        private readonly Unit _unit;

        public UnitPropertiesUpdatePacketBuilder(Unit unit, IEnumerable<ItemProperty> properties)
        {
            _unit = unit;
            _properties = properties;
        }

        public Packet Build()
        {
            var packet = new Packet(ZoneCommand.UpdateStat);
            packet.AppendLong(_unit.Eid);

            foreach (var property in _properties)
            {
                property.AppendToPacket(packet);
            }

            return packet;
        }
    }
}