using System.Collections.Generic;
using Perpetuum.Modules;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;

namespace Perpetuum.Zones.CombatLogs
{
    public sealed class CombatSummary
    {
        private readonly CombatLogHelper _combatLogHelper;
        public Unit Source { get; private set; }

        private double _totalDamage;
        private int _sensorJammerTotal;
        private int _sensorJammerSuccess;
        private int _demobilizer;
        private int _sensorDampener;
        private double _energyDispersion;
        private bool _killingBlow;

        public delegate CombatSummary Factory(Unit source);

        public CombatSummary(Unit source,CombatLogHelper combatLogHelper)
        {
            _combatLogHelper = combatLogHelper;
            Source = source;
        }

        public void HandleCombatEvent(CombatEventArgs e)
        {
            switch (e)
            {
                case DamageTakenEventArgs dte:
                {
                    _totalDamage += dte.TotalDamage;
                    break;
                }
                case SensorJammerEventArgs sje:
                {
                    _sensorJammerTotal++;

                    if (sje.Success)
                        _sensorJammerSuccess++;

                    break;
                }
                case DemobilizerEventArgs de:
                {
                    _demobilizer++;
                    break;
                }
                case SensorDampenerEventArgs sde:
                {
                    _sensorDampener++;
                    break;
                }
                case EnergyDispersionEventArgs ede:
                {
                    _energyDispersion += ede.Amount;
                    break;
                }
                case KillingBlowEventArgs kbe:
                {
                    _killingBlow = true;
                    break;
                }
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
                    {
                        {k.attacker,_combatLogHelper.GetUnitInfo(Source)},
                        {k.damageDone, _totalDamage},
                        {"jammerTotal",_sensorJammerTotal},
                        {"jammer", _sensorJammerSuccess},
                        {"demobilizer", _demobilizer},
                        {"suppressor", _sensorDampener},
                        {"dispersion", _energyDispersion},
                        {"killingBlow", _killingBlow},
                    };

            return dictionary;
        }
    }
}
