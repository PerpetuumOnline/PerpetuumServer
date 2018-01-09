using System;
using System.Collections.Generic;
using Perpetuum.Log;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;

namespace Perpetuum.Zones.PBS
{


    public interface ICoreUseHandler
    {
        void ToWarmUpState();
        void ToActiveState();
        double GetKickStartCoreRatio();

        double GetCoreDemand();

        PBSEnergyState EnergyState { get; set; }
        double LastCoreUse { get; set; }
        double CoreMinimum { get; }
    }

    public class CoreUseHandler<T> : ICoreUseHandler where T : Unit, IPBSUsesCore, IPBSObject
    {
        private readonly Lazy<TimeSpan> _cycleTime;
        private readonly T _sourceUnit;
        private readonly IEnergyStateFactory<T> _factory;

        private readonly FiniteStateMachine<IEnergyState> _sm = new FiniteStateMachine<IEnergyState>();

        public CoreUseHandler(T sourceUnit, IEnergyStateFactory<T> factory)
        {
            _sourceUnit = sourceUnit;
            _factory = factory;
            _cycleTime = new Lazy<TimeSpan>(GetCycleTimeMs);
        }

        public double CoreMinimum { get; set; }

        /// <summary>
        /// 
        /// On Zone enter called
        /// 
        /// </summary>
        public void Init() 
        {
            CoreMinimum = _sourceUnit.ED.Config.CoreConsumption;
            _sm.ChangeState(new DelayedStartState<T>(_sourceUnit));
        }

        public void ToWarmUpState()
        {
            _sm.ChangeState(_factory.CreateWarmUpEnergyState());
        }

        public void ToActiveState()
        {
            _sm.ChangeState(_factory.CreateActiveEnergyState());
        }

        public void OnUpdate(TimeSpan time)
        {
            _sm.Update(time);
        }

       

        public double GetKickStartCoreRatio()
        {
            if (_sourceUnit.ED.Config.coreKickStartThreshold != null)
                return (double) _sourceUnit.ED.Config.coreKickStartThreshold;

            //Logger.Error("no coreKickStartThreshold defined for " + _sourceUnit.EntityDefault.Name);
            return 0.7;
        }



        public double GetCoreDemand()
        {
            //ez a cucc valahogy osszegyujti, hogy mennyi core kell neki, o nem fixet fogyaszt
            double coreDemand;
            if (_sourceUnit.TryCollectCoreConsumption(out coreDemand))
                return coreDemand;

            //de tud fallbackelni is a config ertekre
            return _sourceUnit.GetCoreConsumption();

        }


        public PBSEnergyState EnergyState { get; set; }
        public double LastCoreUse { get; set; }


        //ez megy visitorosba
        public void AddToDictionary(IDictionary<string, object> info)
        {
            info[k.PBSEnergyState] = (int)EnergyState;
            info[k.lastUsedCore] = LastCoreUse;
            info[k.currentCore] = _sourceUnit.Core;
        }

        private TimeSpan GetCycleTimeMs()
        {
            if (_sourceUnit.ED.Config.cycle_time != null)
                return TimeSpan.FromMilliseconds((double) _sourceUnit.ED.Config.cycle_time);

            Logger.Error("consistency error. no cycle_time was defined for definition: " + _sourceUnit.Definition + " " + _sourceUnit.ED.Name);
            return TimeSpan.FromSeconds(30);
        }
       
    }



    public interface IEnergyStateFactory<T> where T : Unit, IPBSUsesCore, IPBSObject
    {
        WarmUpRawCoreState<T> CreateWarmUpEnergyState();
        ActiveRawCoreState<T> CreateActiveEnergyState();
    }


    public abstract class EnergyStateVisitor
    {
        public void VisitEnergyState(IEnergyState state)
        {

        }

        
        public virtual void VisitWarmUpState(IEnergyState state) { }

        public virtual void VisitActiveState(IEnergyState state) { }

    }

   


    public interface IEnergyState : IState
    {
        void AcceptVisitor(EnergyStateVisitor visitor);
    }


    public abstract class CoreStateBase<T> : IEnergyState where T : Unit, IPBSUsesCore, IPBSObject
    {
        private readonly T _owner;

        public CoreStateBase(T owner)
        {
            _owner = owner;
        }

        protected T Owner
        {
            get { return _owner; }
        }

        public virtual void Enter()
        {
            WriteLog(" *** ENTER *** " + GetType().Name + "  " + _owner.ED.Name);
        }

        public virtual void Exit()
        {
            WriteLog(" *** EXIT *** " + GetType().Name + "  " + _owner.ED.Name);
        }

        public void Update(TimeSpan time)
        {
            OnUpdate(time);
        }

        public void AcceptVisitor(EnergyStateVisitor visitor)
        {
            visitor.VisitEnergyState(this);
        }

        private void WriteLog(string message)
        {
            Logger.Info(message);
        }

        protected virtual void OnUpdate(TimeSpan time) { }
    }

    //ezek mennek az olyan nodeoknak akik IPBSCoreUser de nem fixen fogyaszt, pl turret
    public abstract class ActiveRawCoreState<T> : CoreStateBase<T> where T : Unit, IPBSUsesCore, IPBSObject
    {
        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(30));

        protected ActiveRawCoreState(T owner) : base(owner)
        {

        }

        public override void Enter()
        {
            base.Enter();
            Owner.CoreUseHandler.EnergyState = PBSEnergyState.active;
            Owner.SendNodeUpdate();
        }

        protected override void OnUpdate(TimeSpan time)
        {
            ActiveCycleWork(time);
        }

        private void ActiveCycleWork(TimeSpan time)
        {
            _timer.Update(time);
            if (!_timer.Passed) return;
            _timer.Reset();

            if (Owner.Core < Owner.CoreUseHandler.CoreMinimum)
            {
                Owner.CoreUseHandler.ToWarmUpState();
                return;
            }

            Owner.CoreUseHandler.EnergyState = PBSEnergyState.active;

            if (Owner.IsFullyConstructed() && Owner.OnlineStatus)
            {
                OnActiveCycle();
            }
        }

        protected virtual void OnActiveCycle()  { }
    }


    public abstract class WarmUpRawCoreState<T> : CoreStateBase<T> where T : Unit, IPBSUsesCore, IPBSObject
    {
        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(30));

        protected WarmUpRawCoreState(T owner) : base(owner)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Owner.CoreUseHandler.EnergyState = PBSEnergyState.warmup;
            Owner.SendNodeUpdate();
        }

        protected override void OnUpdate(TimeSpan time)
        {
            WarmUpCycleWork(time);
        }

        private void WarmUpCycleWork(TimeSpan time)
        {
            _timer.Update(time);
            if (!_timer.Passed) return;
            _timer.Reset();

            Owner.CoreUseHandler.EnergyState = PBSEnergyState.warmup;
            Owner.CoreUseHandler.LastCoreUse = 0;

            if (Owner.CorePercentage > Owner.CoreUseHandler.GetKickStartCoreRatio())
            {
                Owner.CoreUseHandler.ToActiveState();
                return;
            }

            OnWarmUpCycle();
        }

        protected virtual void OnWarmUpCycle() { }
    }



    //ezek mennek a IPBSCoreUse-nak
    public abstract class ActiveCoreUserNodeState<T> : ActiveRawCoreState<T> where T : Unit, IPBSUsesCore, IPBSObject
    {
        protected ActiveCoreUserNodeState(T owner) : base(owner)
        {
        }

        protected sealed override void OnActiveCycle()
        {
            var coreUsage = Owner.CoreUseHandler.GetCoreDemand();

            if (coreUsage > 0)
            {
                // ha akar fogyasztani valamennyit, de nincs eleg
                if (Owner.Core < coreUsage)
                {
                    Owner.CoreUseHandler.ToWarmUpState(); //nincs eleg core, atmegy warmupba
                    return;
                }

                Owner.Core -= coreUsage;

                Owner.CoreUseHandler.LastCoreUse = coreUsage;

                //fogyasztott is coret, akkor csinalja meg ami a feladata
                PostCoreSubtract();
            }
        }

        protected virtual void PostCoreSubtract() { }
    }

    public abstract class WarmUpCoreUserNodeState<T> : WarmUpRawCoreState<T> where T : Unit, IPBSUsesCore, IPBSObject
    {
        protected WarmUpCoreUserNodeState(T owner) : base(owner)
        {
        }

        protected override void OnWarmUpCycle()
        {

        }
    }

    public class DelayedStartState<T> : CoreStateBase<T> where T : Unit, IPBSUsesCore, IPBSObject
    {
        private readonly  IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(30+FastRandom.NextInt(0,15)));

        public DelayedStartState(T owner) : base(owner)
        {
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _timer.Update(time);
            if (!_timer.Passed) return;
            _timer.Reset();

            Logger.DebugInfo($" ---------- KEZDUNK ---------- [{Owner.InfoString}]");

            //itt ez az egesz arra kell, hogy amikor megindul a server akkor belemegy valami statebe
            //ott hasznal zonat, aztan csak nez, hogy nincs kinn az objekt, igen, mert az is epp pakolodik ki
            //
            //ez ugyan csak ragtapasz.


            if (Owner.Core < Owner.CoreUseHandler.CoreMinimum)
            {
                Owner.CoreUseHandler.ToWarmUpState();
                return; 
            }
            
            Owner.CoreUseHandler.ToActiveState();

        }
    }


}

   

