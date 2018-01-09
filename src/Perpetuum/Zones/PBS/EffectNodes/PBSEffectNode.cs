using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Zones.PBS.EffectNodes
{
    /// <summary>
    /// Abstract effect emitter class
    /// </summary>
    public abstract class PBSEffectNode : PBSActiveObject, IPBSUsesCore, IPBSAcceptsCore, IStandingController
    {
        private readonly CoreUseHandler<PBSEffectNode> _coreUseHandler;
        
        private readonly PBSStandingController<PBSEffectNode> _standingController;

       
       
        protected abstract IEnumerable<Unit> GetTargetUnits();
        protected abstract double CollectCoreConsumption();

        private readonly IDynamicProperty<int /* effectType */> _currentEffect;

        protected PBSEffectNode()
        {
            _standingController = new PBSStandingController<PBSEffectNode>(this) { AlwaysEnabled = true };
            _coreUseHandler = new CoreUseHandler<PBSEffectNode>(this,new EnergyStateFactory(this));

            _currentEffect = DynamicProperties.GetProperty<int>(k.currentEffect);
            _currentEffect.PropertyChanging += (property, value) =>
            {
                var effectType = (EffectType) value;

                if (effectType == EffectType.undefined)
                    effectType = AvailableEffects.FirstOrDefault();

                if (!AvailableEffects.Contains(effectType))
                    Logger.Error("PBSEffectNode: invalid effect type! type:" + effectType);

                RemoveCurrentEffect();
                return (int) effectType;
            };

            _currentEffect.PropertyChanged += property =>
            {
                OnEffectChanged();
            };
        }

        public ICoreUseHandler CoreUseHandler { get { return _coreUseHandler; } }

        public double StandingLimit
        {
            get { return _standingController.StandingLimit; }
            set { _standingController.StandingLimit = value; }
        }

        public bool StandingEnabled
        {
            get { return _standingController.Enabled; }
            set { _standingController.Enabled = value; }
        }

        public bool TryCollectCoreConsumption(out double coreDemand)
        {
            coreDemand = CollectCoreConsumption();
            return true;
        }

        /// <summary>
        /// Decides if the target player will receive the effect based on corporation standings standing
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        protected bool IsMyTarget(Unit unit)
        {
            var player = unit as Player;
            if (player == null)
                return false;

            //ha nincs standing allitva akkor csak a sajat corpra tolja
            if (!StandingEnabled)
            {
                return player.CorporationEid == Owner;
            }

            //ha van standing beallitva akkor tolja masokra is
            return player.IsStandingMatch(Owner, StandingLimit);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _coreUseHandler.OnUpdate(time);
            base.OnUpdate(time);
           
        }

        /// <summary>
        /// Returns the effect list from the entitydefaults options
        /// </summary>
        /// <value></value>
        public EffectType[] AvailableEffects
        {
            get
            {
#if DEBUG
                //checks effect existense
                foreach (var effectType in ED.Options.Effects)
                {
                    if (!Enum.IsDefined(typeof (EffectType), effectType))
                    {
                        Logger.Error("consistency error. no such effect defined:" + effectType + " " + ED.Name + " " + Definition);
                    }
                }
#endif
                return ED.Options.Effects;
            }
        }


        public EffectType CurrentEffectType
        {
            private get { return (EffectType) _currentEffect.Value; }
            set { _currentEffect.Value = (int) value; }
        }

        private void OnEffectChanged()
        {
            if (this.IsFullyConstructed() && OnlineStatus && _coreUseHandler.EnergyState == PBSEnergyState.active)
            {
                ApplyCurrentEffect();
            }
        }

        public void RemoveCurrentEffect()
        {
            EffectHandler.RemoveEffectsByType(CurrentEffectType);
            OnEffectRemoved();
        }

        public void ApplyCurrentEffect()
        {
            if (EffectHandler.ContainsEffect(CurrentEffectType))
                return;

            var builder = NewEffectBuilder().SetType(CurrentEffectType)
                .SetOwnerToSource()
                .EnableModifiers(false)
                .WithTargetSelector(zone => GetTargetUnits());

            OnApplyEffect(builder);
            ApplyEffect(builder);
        }


        protected virtual void OnApplyEffect(EffectBuilder builder) {}
        protected virtual void OnEffectRemoved() { }

        private void Init()
        {

            EffectType currentEffect;

            if (DynamicProperties.Contains(k.currentEffect))
            {
                currentEffect = (EffectType)DynamicProperties.GetOrAdd<int>(k.currentEffect);
            }
            else
            {
                currentEffect = AvailableEffects.FirstOrDefault();
            }

            CurrentEffectType = currentEffect;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            Init();

            _coreUseHandler.Init();

            //majd itt fog onlineba atmenni
            base.OnEnterZone(zone, enterType);
        }

        public override void OnInsertToDb()
        {
            SaveToDb();
            base.OnInsertToDb();
        }

        public override void OnUpdateToDb()
        {
            SaveToDb();
            base.OnUpdateToDb();
        }

        private void SaveToDb()
        {
            DynamicProperties.Update(k.currentEffect, (int) CurrentEffectType);
        }


        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();

            info.Add(k.currentEffect, (int)CurrentEffectType);

            _coreUseHandler.AddToDictionary(info);
            

            return info;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            info.Add(k.currentEffect, (int)CurrentEffectType);
            info.Add(k.PBSEnergyState, 0);
            //info.Add(k.emitRadius, EmitRadius);

            
            _coreUseHandler.AddToDictionary(info);

            return info;
        }


        private int _workingEffectChange;

        protected override void OnOnlineStatusChanged(bool onlineStatus)
        {

            if (Interlocked.CompareExchange(ref _workingEffectChange, 1, 0) == 1)
                return;

            try
            {


                if (onlineStatus)
                {
                    //es meg energiaval is jol allunk
                    if (_coreUseHandler.EnergyState == PBSEnergyState.active)
                    {
                        ApplyCurrentEffect();
                    }
                    else
                    {
                        RemoveCurrentEffect();
                    }
                }
                else
                {
                    RemoveCurrentEffect();
                }



            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
            finally
            {
                _workingEffectChange = 0;
            }
        }

        private class  EnergyStateFactory : IEnergyStateFactory<PBSEffectNode>
        {
            private readonly PBSEffectNode _node;

            public EnergyStateFactory(PBSEffectNode node)
            {
                _node = node;
            }

            public WarmUpRawCoreState<PBSEffectNode> CreateWarmUpEnergyState()
            {
                return new WarmUpEnergyState(_node);
            }

            public ActiveRawCoreState<PBSEffectNode> CreateActiveEnergyState()
            {
                return new ActiveEnergyState(_node);
            }
        }

        private class ActiveEnergyState : ActiveCoreUserNodeState<PBSEffectNode>
        {
            public ActiveEnergyState(PBSEffectNode owner) : base(owner)
            {
            }

            public override void Enter()
            {
                base.Enter();
                ActiveWork();
            }

            protected override void PostCoreSubtract()
            {
               ActiveWork();
            }

            private void ActiveWork()
            {
                if (Owner.IsFullyConstructed() && Owner.OnlineStatus)
                {
                    Owner.ApplyCurrentEffect();
                }
                else
                {
                    Owner.RemoveCurrentEffect();
                }
                
            }

        }

        private class WarmUpEnergyState  : WarmUpCoreUserNodeState<PBSEffectNode>
        {
            public WarmUpEnergyState(PBSEffectNode owner) : base(owner)
            {
            }

            public override void Enter()
            {
                base.Enter();

                Owner.RemoveCurrentEffect();
            }

            protected override void OnWarmUpCycle()
            {
                Owner.RemoveCurrentEffect();
            }
        }
        
    }
}
