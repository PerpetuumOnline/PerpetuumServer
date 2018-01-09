using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Zones.PBS.EffectNodes
{
    /// <summary>
    /// This node emits an effect in a range to players
    /// </summary>
    public class PBSEffectEmitter : PBSEffectNode
    {
        protected override IEnumerable<Unit> GetTargetUnits()
        {
            return GetTargetsByPosition();
        }

        //ez csak pelda, nem kell
        protected override void OnPropertyChanged(ItemProperty property)
        {
            if (property.Field == AggregateField.core_current)
            {
                //Console.WriteLine("Valtozott a core! " + property.Value);
            }

            base.OnPropertyChanged(property);
        }



        private int _lastCollectedPlayers;


        /// <summary>
        /// Collect the targets to apply the effect on.
        /// - targets in range
        /// - standing filter
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Unit> GetTargetsByPosition()
        {
           
            foreach (var player in Zone.Units.OfType<Player>().WithinRange(CurrentPosition, EmitRadius))
            {
                if (IsMyTarget(player))
                {
                    Interlocked.Increment(ref _lastCollectedPlayers);
                    yield return player;
                }

            }
            
            
        }


        private const double tickDivider = 15; //30sec VS 2sec effect -> igy jon ki a core consumption egy ciklusra

        protected override double CollectCoreConsumption()
        {
            var lastCollected = Interlocked.Exchange(ref _lastCollectedPlayers, 0);

            if (lastCollected <= 0) return 0.0;

            var coreDemand = (this.GetCoreConsumption() * lastCollected) / tickDivider;
            
            return coreDemand;
        }

        private int _emitRadius;

        private int EmitRadius
        {
            get
            {
                if (_emitRadius <= 0)
                {
                    if (ED.Config.emitRadius != null)
                    {
                        _emitRadius = (int)ED.Config.emitRadius;
                    }
                    else
                    {
                        Logger.Error("no emitradius defined for " + this);
                        _emitRadius = 10;
                    }

                }

                return _emitRadius;

            }

        }

        protected override void OnApplyEffect(EffectBuilder builder)
        {
            builder.WithRadius(EmitRadius);
            base.OnApplyEffect(builder);
        }

        protected override void OnEffectRemoved()
        {
            base.OnEffectRemoved();

            _lastCollectedPlayers = 0;
        }
    }



}
