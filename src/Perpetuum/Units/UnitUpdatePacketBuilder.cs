using Perpetuum.Builders;
using Perpetuum.Zones;

namespace Perpetuum.Units
{
    public class UnitUpdatePacketBuilder : IBuilder<Packet>
    {
        private readonly UpdatePacketControl _control;
        private readonly Unit _unit;

        public UnitUpdatePacketBuilder(Unit unit, UpdatePacketControl control = UpdatePacketControl.Undefined)
        {
            _unit = unit;
            _control = control;
        }

        public Packet Build()
        {
            var packet = new Packet(ZoneCommand.UpdateUnit);

            packet.AppendLong(_unit.Eid);
            packet.AppendPosition(_unit.CurrentPosition);
            packet.AppendByte((byte)(_unit.CurrentSpeed * 255));
            packet.AppendByte((byte)(_unit.Orientation * byte.MaxValue));
            packet.AppendByte((byte)(_unit.Direction * byte.MaxValue));
            _unit.States.AppendToPacket(packet);
            packet.AppendByte((byte)(_control));

            _unit.OptionalProperties.GetUpdatedProperties().WriteToStream(packet);
            return packet;
        }
    }
}