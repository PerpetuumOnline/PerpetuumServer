using System;
using Perpetuum.Builders;

namespace Perpetuum.Zones.Beams
{
    public class BeamPacketBuilder : IBuilder<Packet>
    {
        private readonly Beam _beam;

        public BeamPacketBuilder(Beam beam)
        {
            _beam = beam;
        }

        public Packet Build()
        {
            var packet = new Packet(ZoneCommand.Beam);

            packet.AppendLong(_beam.Id);
            packet.AppendInt((int)_beam.Type);
            packet.AppendByte(_beam.Slot);
            packet.AppendLong(_beam.SourceEid);
            packet.AppendPosition(_beam.SourcePosition);
            packet.AppendLong(_beam.TargetEid);
            packet.AppendPosition(_beam.TargetPosition);
            packet.AppendInt((int) _beam.Duration.TotalMilliseconds);

            var elapsed = DateTime.Now.Subtract(_beam.Created);
            packet.AppendInt((int)elapsed.TotalMilliseconds);
            packet.AppendByte((byte)_beam.State);
            packet.AppendDouble(_beam.BulletTime);

            return packet;
        }
    }
}