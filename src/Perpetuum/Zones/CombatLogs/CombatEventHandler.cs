using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;

namespace Perpetuum.Zones.CombatLogs
{
    public delegate void CombatEventHandler<in T>(Unit source, T e) where T : CombatEventArgs;
}