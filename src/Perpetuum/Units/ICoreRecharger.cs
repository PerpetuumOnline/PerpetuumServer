using System;

namespace Perpetuum.Units
{
    public interface ICoreRecharger
    {
        void RechargeCore(Unit unit, TimeSpan elapsedTime);
    }
}