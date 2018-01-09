using System;

namespace Perpetuum.Zones.Terrains
{
    public class TerraformableAltitude : AltitudeLayer
    {
        private const ushort BARRIER_MINIMUM = 1850;
        private const ushort BARRIER_MAXIMUM = 30000; //32768at SOHA nem lephetjuk at

        private readonly ILayer<ushort> _blend;

        public TerraformableAltitude(ILayer<ushort> original,ILayer<ushort> blend,ushort[] rawData) : base(rawData,original.Width,original.Height)
        {
            OriginalAltitude = original;
            _blend = blend;
            Barrier = new Layer<BarrierInfo>(LayerType.Barrier, Width, Height);

            CalculateBarrier();
        }

        public ILayer<ushort> OriginalAltitude { get; }
        public ILayer<BarrierInfo> Barrier { get; }

        private void CalculateBarrier()
        {
            for (var i = 0; i < Barrier.RawData.Length; i++)
            {
                var originalValue = OriginalAltitude.RawData[i];
                var blendValue = _blend.RawData[i] / (double)ushort.MaxValue;

                var minBarrier = BARRIER_MINIMUM.Mix(originalValue, blendValue);
                var maxBarrier = BARRIER_MAXIMUM.Mix(originalValue, blendValue);

                minBarrier = Math.Min(minBarrier, originalValue);
                maxBarrier = Math.Max(maxBarrier, originalValue);

                var barrierInfo = new BarrierInfo(minBarrier, maxBarrier);
                Barrier.RawData[i] = barrierInfo;
            }
        }

        protected override void OnUpdating(int x, int y, ref ushort value)
        {
            var barrier = Barrier.GetValue(x, y);
            var currentAltitude = GetValue(x, y);
            if (currentAltitude >= barrier.min && currentAltitude <= barrier.max)
            {
                value = value.Clamp(barrier.min, barrier.max);
            }

            base.OnUpdating(x, y, ref value);
        }

        public struct BarrierInfo
        {
            public readonly ushort min;
            public readonly ushort max;

            public BarrierInfo(ushort min, ushort max)
            {
                this.min = min;
                this.max = max;
            }
        }
    }
}