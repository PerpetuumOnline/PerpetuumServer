using Perpetuum.Zones.Beams;

namespace Perpetuum.Zones
{
    public sealed class ZoneEnterInfo
    {
        public ZoneEnterInfo()
        {
            EnterType = ZoneEnterType.Default;
        }

        public ZoneEnterType EnterType { get; internal set; }
        public IBeamBuilder EnterBeamBuilder { get; internal set; }
    }
}