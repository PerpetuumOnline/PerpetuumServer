using Perpetuum.Builders;

namespace Perpetuum.Zones.Effects
{
    public class EffectPacketBuilder : IBuilder<Packet>
    {
        private readonly Effect _effect;
        private readonly bool _apply;

        public EffectPacketBuilder(Effect effect, bool apply)
        {
            _effect = effect;
            _apply = apply;
        }

        public Packet Build()
        {
            if (_apply)
            {
                var packet = new Packet(ZoneCommand.EffectOn);
                _effect.AppendToStream(packet);
                return packet;
            }
            else
            {
                var packet = new Packet(ZoneCommand.EffectOff);
                packet.AppendInt(_effect.Id);
                packet.AppendLong(_effect.Owner.Eid);
                return packet;
            }
        }
    }
}