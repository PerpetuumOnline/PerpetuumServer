using System;

namespace Perpetuum.Units
{
    public class CoreRecharger : ICoreRecharger
    {
        private static readonly TimeSpan _coreRechargeFrequency = TimeSpan.FromSeconds(5); // core toltese
        public static readonly ICoreRecharger None = new NullCoreRecharger();

        private TimeSpan _elapsed;

        public void RechargeCore(Unit unit, TimeSpan elapsedTime)
        {
            _elapsed += elapsedTime;

            if (_elapsed < _coreRechargeFrequency)
                return;

            _elapsed -= _coreRechargeFrequency;

            if (unit.Core >= unit.CoreMax)
                return;

            var timeIncrement = unit.CoreRechargeTime.TotalMilliseconds / _coreRechargeFrequency.TotalMilliseconds;
            var fillRate = unit.Core.Ratio(unit.CoreMax);

            var reversedTime = MathHelper.ReverseTensionedEaseInEaseOut(fillRate, 1.0);
            var value = reversedTime + (1 / timeIncrement);
            var nextRelative = MathHelper.TensionedEaseInEaseOut(value, 1.0);

            unit.Core = nextRelative * unit.CoreMax;
        }

        private class NullCoreRecharger : ICoreRecharger
        {
            public void RechargeCore(Unit unit, TimeSpan elapsedTime)
            {
            }
        }
    }
}