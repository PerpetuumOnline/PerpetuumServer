using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS
{
    public interface IReinforceState : IState
    {
        void ToNormal();
        void ToVulnerable();
        void ToReinforce(Unit attacker);
        bool IsReinforced { get; }
        bool IsVulnerable { get; }
        bool CanBeKilled { get; }
        DateTime ReinforceEndTime { get; }
        void OnSave();
        void AddToDictionary(IDictionary<string, object> info);
    }

    public class NullReinforceHandler : IPBSReinforceHandler
    {
        public void SetReinforceOffset(Character issuer, int offset)
        {
        }

        public DateTime? ReinforceEnd { get; private set; }
        public DateTime GetReinforceDetails()
        {
            return default(DateTime);
        }

        public int ReinforceOffsetHours { get; private set; }
        public int ReinforceCounter { get; set; }
        public void ForceDailyOffset(int forcedOffsetWithinDay)
        {
        }

        public IReinforceState CurrentState { get; private set; }
    }


    /// <summary>
    /// Handles reinforcing of a pbs object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PBSReinforceHandler<T> : IPBSReinforceHandler where T : Unit,IPBSObject
    {
        private readonly T _pbsUnit;
        private readonly FiniteStateMachine<IReinforceState> _fsm = new FiniteStateMachine<IReinforceState>();

        private int _offsetHoursWithinDay = 20;
        private DateTime _nextReinforceCounterIncrease;
        private int _currentReinforceCounter;
       
        public PBSReinforceHandler(T pbsUnit)
        {
            _pbsUnit = pbsUnit;
            _pbsUnit.PropertyChanged += OnUnitPropertyChanged;
            _fsm.ChangeState(new NormalState(this));
        }

        /// <summary>
        /// Damage triggers reinforce
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="property"></param>
        private void OnUnitPropertyChanged(Item unit, ItemProperty property)
        {
            if (property.Field == AggregateField.armor_current)
            {
                if (property.Value <= 0)
                {
                    CurrentState.ToReinforce(null);
                }
            }
        }

        public bool ZoneReady;

        public void OnUpdate(TimeSpan time)
        {
            ZoneReady = true;

            _fsm.Update(time);
        }
       
        
        private void SetNextReinforceCounterIncreaseFromNow(int minutes)
        {
            _nextReinforceCounterIncrease = DateTime.Now.AddMinutes(minutes);

            //Logger.Info("next reinforce counter increase in " + minutes + " minutes    eid:" + _pbsUnit.Eid + "  " + _pbsUnit.EntityDefault.Name + " n:" +_pbsUnit.Name);

        }
        

        public DateTime? ReinforceEnd
        {
            get
            {
                var t = CurrentState.ReinforceEndTime;

                if (t.Equals(default(DateTime))) return null;

                return t;
            }
        }

        //transfers the data from this node to an other
        public DateTime GetReinforceDetails()
        {
            return CurrentState.ReinforceEndTime;
        }

        //the client sets it - in hours
        public int ReinforceOffsetHours
        {
            get { return _offsetHoursWithinDay; }
        }

        public int ReinforceCounter
        {
            get { return _currentReinforceCounter; }
            set { _currentReinforceCounter = value; }
        }

        public void ForceDailyOffset(int forcedOffsetWithinDay)
        {
            _offsetHoursWithinDay = forcedOffsetWithinDay;
        }

        public IReinforceState CurrentState
        {
            get { return _fsm.Current; }
        }


        private int _reinforceCounterMax = -1;
        private int GetReinforceCounterMax()
        {
            return PBSHelper.LazyReinforceCounterMax(_pbsUnit, ref _reinforceCounterMax);
        }

       
        private void IncreaseReinforceCounter()
        {
            _currentReinforceCounter = (_currentReinforceCounter + 1).Clamp(0, GetReinforceCounterMax());
            SetReinforceCounter();
        }

        private void DecreaseReinforceCounter()
        {
            _currentReinforceCounter = (_currentReinforceCounter - 1).Clamp(0, GetReinforceCounterMax());
            SetReinforceCounter();
        }

        private void SetReinforceCounter()
        {
            //logoljon meg minden %%%

            _pbsUnit.DynamicProperties.Update(k.reinforceCounter,_currentReinforceCounter);
        }

        


        private void StartAsyncLog(PBSLogType logType)
        {
            Task.Run(() =>
            {
                try
                {
                    LogWrite(logType);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            });
        }


        private void LogWrite(PBSLogType logType)
        {
            var zone = _pbsUnit.Zone;
            if (zone != null)
            {
                PBSHelper.WritePBSLog(logType, _pbsUnit.Eid, _pbsUnit.Definition, _pbsUnit.Owner, zoneId: zone.Id);
            }
        }


        private abstract class ReinforceStateBase : IReinforceState
        {
            private readonly PBSReinforceHandler<T> _reinforceHandler;

            public ReinforceStateBase(PBSReinforceHandler<T> reinforceHandler)
            {
                _reinforceHandler = reinforceHandler;
            }

            protected PBSReinforceHandler<T> ReinforceHandler
            {
                get { return _reinforceHandler; }
            }

            public virtual DateTime ReinforceEndTime
            {
                get { return default(DateTime); }
            }

            public virtual void OnSave()
            {
                ReinforceHandler._pbsUnit.DynamicProperties.Update(k.isReinforced, IsReinforced ? 1 : 0);
            }

            public virtual bool IsReinforced
            {
                get { return false; }
            }

            public virtual bool IsVulnerable
            {
                get { return false; }
            }

            public abstract bool CanBeKilled { get; }

            //the node info requires base call at the end always!
            public virtual void Enter()
            {
                if (ReinforceHandler.ZoneReady)
                {
                    WriteLog(" >>> reinforce ENTER " + GetType().Name + " " + _reinforceHandler._pbsUnit.ED.Name);

                    ReinforceHandler._pbsUnit.SendNodeUpdate();

                    //save to sql %%% ezzel rontom el? ettol ment turret fejet a db-be?
                    Entity.Repository.Update(ReinforceHandler._pbsUnit);
                }
            }

            //the node info requires base call at the end always!
            public virtual void Exit()
            {
                if (ReinforceHandler.ZoneReady)
                {
                    WriteLog(" >>> reinforce EXIT  " + GetType().Name + " " + _reinforceHandler._pbsUnit.ED.Name);
                    ReinforceHandler._pbsUnit.SendNodeUpdate();
                }
            }

            public virtual void Update(TimeSpan time)
            {
            }

            public virtual void AddToDictionary(IDictionary<string, object> info)
            {
                info.Add(k.offsetWithinDay, _reinforceHandler._offsetHoursWithinDay);
                info.Add(k.reinforceCounter, _reinforceHandler._currentReinforceCounter);
                info.Add(k.nextReinforceIncrease, _reinforceHandler._nextReinforceCounterIncrease);
            }

            public virtual void ToNormal()
            {
                _reinforceHandler._fsm.ChangeState(new NormalState(_reinforceHandler));
            }

            public virtual void ToReinforce(Unit attacker)
            {
                _reinforceHandler._fsm.ChangeState(new ActiveReinforceState(_reinforceHandler,attacker));

            }

            public virtual void ToVulnerable()
            {
                _reinforceHandler._fsm.ChangeState(new VulnerableState(_reinforceHandler));
            }
            
            protected void WriteLog(string message)
            {
                Logger.Info(message);
            }

        }


        //kezdo allapot 
        private class NormalState : ReinforceStateBase
        {
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(10+FastRandom.NextInt(0,3)));

            public NormalState(PBSReinforceHandler<T> reinforceHandler) : base(reinforceHandler) {}

            public override void Enter()
            {
                ReinforceHandler._pbsUnit.DamageTaken += OnUnitDamageTaken;
                base.Enter();
            }

            public override void Exit()
            {
                ReinforceHandler._pbsUnit.DamageTaken -= OnUnitDamageTaken;
                base.Exit();
            }

            public override bool CanBeKilled
            {
                //if can't switch to reinforce, then it can be killed
                get { return !CanSwitchToReinforce; }
            }

            private void OnUnitDamageTaken(Unit owner, Unit attacker, DamageTakenEventArgs e)
            {
                if (owner.ArmorPercentage >= REINFORCE_THRESHOLD)
                    return;

                ToReinforce(attacker);
            }
           
            //already in this state
            public override void ToNormal() {}

            //only from reinforce
            public override void ToVulnerable() {}

            public override void ToReinforce(Unit attacker)
            {
                if (!CanSwitchToReinforce) return;
                base.ToReinforce(attacker);
            }

            private bool CanSwitchToReinforce
            {
                get
                {
                    if (!ReinforceHandler._pbsUnit.IsFullyConstructed())
                        return false;

                    if (ReinforceHandler._pbsUnit.IsOrphaned)
                        return false;

                    if (ReinforceHandler.ReinforceCounter <= 0) return false;

                    return true;
                }
            }


            public override void Update(TimeSpan time)
            {
                base.Update(time);

                _timer.Update(time);
                if (!_timer.Passed) return;
                _timer.Reset();
                

                if (ReinforceHandler._nextReinforceCounterIncrease < DateTime.Now)
                {
                    ReinforceHandler.SetNextReinforceCounterIncreaseFromNow(REINFORCE_COUNTER_INCREASE_MINUTES);

                    var oldValue = ReinforceHandler.ReinforceCounter;
                    ReinforceHandler.IncreaseReinforceCounter();
                    var newValue = ReinforceHandler.ReinforceCounter;

                    if (oldValue != newValue)
                    {
                        WriteLog("reinforce counter increased. " + ReinforceHandler._pbsUnit.ED.Name + "  " + oldValue + "->" + newValue);
                    }
                }

            }
        }





        private class ActiveReinforceState : ReinforceStateBase
        {
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(10 + FastRandom.NextInt(0, 3)));
            private DateTime _reinforceEnd;
            private readonly Character _reinforceStartedBy;

            public ActiveReinforceState(PBSReinforceHandler<T> reinforceHandler, Unit attacker) : base(reinforceHandler)
            {
                if (attacker is Player player)
                {
                    _reinforceStartedBy = player.Character;
                }
            }

            public void Init(Unit owner)
            {
                if (owner.DynamicProperties.Contains(k.reinforceEnd))
                {
                    var storedReinforceEnd = owner.DynamicProperties.GetOrAdd<DateTime>(k.reinforceEnd);
                    _reinforceEnd = storedReinforceEnd;
                    Logger.Info("active reinforce state was set by init " + owner);
                }
            }

            public override void OnSave()
            {
                base.OnSave();
                ReinforceHandler._pbsUnit.DynamicProperties.Update(k.reinforceEnd, _reinforceEnd);
            }

            public override DateTime ReinforceEndTime => _reinforceEnd;

            //already in this state
            public override void ToReinforce(Unit attacker) { }
            
            public override bool IsReinforced => true;

            public override bool CanBeKilled => false;

            public override void Enter()
            {
                ReinforceHandler._pbsUnit.OrphanedStateChanged += OnOrphanedStateChanged;
                Task.Run(() => DoEnter()).ContinueWith(t => base.Enter());
            }

            public override void Exit()
            {
                ReinforceHandler._pbsUnit.OrphanedStateChanged -= OnOrphanedStateChanged;
                ReinforceHandler._pbsUnit.DynamicProperties.Update(k.isReinforced, 0);
                ReinforceHandler._pbsUnit.DynamicProperties.Update(k.reinforceEnd, default(DateTime));
                ReinforceHandler._pbsUnit.States.Reinforced = false;

                ReinforceHandler.StartAsyncLog(PBSLogType.reinforceEnd);
                ReinforceHandler.SetNextReinforceCounterIncreaseFromNow(VULNERABLE_LENGTH_MINUTES);

                if (PBSHelper.IsOfflineOnReinforce(ReinforceHandler._pbsUnit))
                {
                    //go online if the node is leaving reinforce
                    ReinforceHandler._pbsUnit.SetOnlineStatus(true, false, true);
                }

                base.Exit();
            }

            private void OnOrphanedStateChanged(Unit unit, bool orphanedState)
            {
                if (orphanedState)
                {
                    ToNormal();
                }
            }

          
            private void DoEnter()
            {
                var pbsUnitObject = ReinforceHandler._pbsUnit;

                var zone = pbsUnitObject.Zone;
                if (zone == null)
                    return;

                //ettol van rajta a grafikai effekt
                pbsUnitObject.States.Reinforced = true;

                pbsUnitObject.DynamicProperties.Set(k.isReinforced, 1);

                //if already set by the init, then we skip this part
                if (_reinforceEnd.Equals(default(DateTime)))
                {
                    Logger.Info(" fresh reinforce start for " + pbsUnitObject);

                    var killer = _reinforceStartedBy?.Id;

                    //sql log
                    PBSHelper.WritePBSLog(PBSLogType.reinforceStart, pbsUnitObject.Eid, pbsUnitObject.Definition, pbsUnitObject.Owner, zoneId:zone.Configuration.Id, killerCharacterId:killer);
                    

                    var oldCounter = ReinforceHandler.ReinforceCounter;
                    //elveszunk egy lehetoseget
                    ReinforceHandler.DecreaseReinforceCounter();
                    var newCounter = ReinforceHandler.ReinforceCounter;

                    //kozoljuk, h mikor lesz a kovetkezo reinforce increase
                    ReinforceHandler.SetNextReinforceCounterIncreaseFromNow(VULNERABLE_LENGTH_MINUTES + REINFORCE_LENGTH_MINUTES);

                    Logger.Info("reinforce counter from:" + oldCounter + " to:" + newCounter + " " + pbsUnitObject.ED.Name);

                    var networkNodes = pbsUnitObject.ConnectionHandler.NetworkNodes.ToArray();

                    if (networkNodes.Length > 1)
                    {
                        //ez egy networkben levo node

                        //ez a bazis a networkben
                        var pbsDockingBase = networkNodes.FirstOrDefault(o => o is PBSDockingBase);

                        if (pbsDockingBase != null)
                        {
                            //van bazis a networkben
                            //abban van a napi offset beallitva

                            if (!pbsDockingBase.Equals(pbsUnitObject))
                            {
                                //not myself
                                ReinforceHandler._offsetHoursWithinDay = pbsDockingBase.ReinforceHandler.ReinforceOffsetHours; //transfer the daily offset from the base

                                Logger.Info("daily offset was transferred from base to:" + pbsUnitObject.ED.Name + " " + pbsUnitObject.Name);
                            }


                            //search for the first reinforced node
                            foreach (var networkNode in networkNodes)
                            {
                                if (networkNode.Equals(pbsUnitObject))
                                    continue;

                                if (!networkNode.ReinforceHandler.CurrentState.IsReinforced)
                                    continue;

                                //ebben a nodeban van, hogy mikor lesz vege a reinforcenak
                                var tmpEnd = networkNode.ReinforceHandler.GetReinforceDetails();


                                if (!tmpEnd.Equals(default(DateTime)) && DateTime.Now < tmpEnd)
                                {
                                    _reinforceEnd = tmpEnd;

                                    Logger.Info(" ");
                                    Logger.Info(" reinforceEnd:" + _reinforceEnd);
                                    Logger.Info("reinforce timer transferred to:" + pbsUnitObject);

                                    GoOfflineOnReinforce(pbsUnitObject);

                                    return;
                                }
                            }
                        }
                    }

                    //ez az ag amikor egyedul van a networkben a node amit megtamadtak
                    //vagy nem volt node ami meghatarozta volna az ertekeket


                    _reinforceEnd = CalculateReinforceEnd();

                    Logger.Info("reinforceEnd:" + _reinforceEnd);
                    Logger.Info("node switches to reinforced: " + pbsUnitObject.Name + " " + pbsUnitObject.Eid + " " + pbsUnitObject.ED.Name);

                }
                else
                {
                    Logger.Info("ACTIVE REINFORCE STATE: skipping inital round, this is a server restart " + pbsUnitObject.Eid + " " + pbsUnitObject.Name);
                }

                GoOfflineOnReinforce(pbsUnitObject);


            }

            private void GoOfflineOnReinforce(T pbsUnitObject)
            {
                if (PBSHelper.IsOfflineOnReinforce(pbsUnitObject))
                {
                    //go offline if the node is in reinforce
                    pbsUnitObject.SetOnlineStatus(false, false, true);
                }
            }


          
        
            private int _inProgress;

            public override void Update(TimeSpan time)
            {
                _timer.Update(time);
                if (!_timer.Passed)
                    return;

                _timer.Reset();

                if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
                    return;

                Task.Run(() =>
                {
                    try
                    {
                        DoReinforceEndCheck();
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }).ContinueWith(t=> { _inProgress = 0; });
            }
          

            private void DoReinforceEndCheck()
            {
                var pbsUnitObject = ReinforceHandler._pbsUnit;

                var zone = pbsUnitObject.Zone;
                if (zone == null)
                    return;

                //if reinforce end is defined and is passed 
                if (DateTime.Now <= _reinforceEnd)
                    return;

                //reinforced state is ending

                if (ReinforceHandler.ReinforceCounter > 0)
                {
                    // do next round
                    Logger.Info("reinforce rounds left:" + ReinforceHandler.ReinforceCounter + " " + pbsUnitObject);
                    ToNormal(); //vissza restbe
                }
                else
                {
                    // vulnerable 
                    Logger.Info("node switches to vulnerable: " + pbsUnitObject);
                    ToVulnerable();
                }

            }

            public override void AddToDictionary(IDictionary<string, object> info)
            {
                base.AddToDictionary(info);

                info.Add(k.reinforceEnd,_reinforceEnd);
                info.Add(k.isReinforced, 1);
            }

            private DateTime CalculateReinforceEnd()
            {
                var reinforceEndDay = DateTime.Now.AddMinutes(REINFORCE_LENGTH_MINUTES);

                ModifyReinforceEnd(ref reinforceEndDay);

                Logger.Info("calculated reinforce end: " + reinforceEndDay + " " + ReinforceHandler._pbsUnit);
                return reinforceEndDay;
            }

            /// <summary>
            /// Ez szamolja ki a utolso napon az idopontot
            /// </summary>
            /// <returns></returns>
            //[Conditional("RELEASE")]
            private void ModifyReinforceEnd(ref DateTime reinforceDay)
            {
                var offsetHours = ReinforceHandler._offsetHoursWithinDay;
                var dayZero = reinforceDay.Date;
                var resultDate = dayZero.AddHours(offsetHours);

#if DEBUG
                Logger.Warning("incoming date: " + $"{reinforceDay:G}");
                Logger.Warning("offsetHours: " + offsetHours);
                Logger.Warning("day zero: " + $"{dayZero:G}");
                Logger.Warning("with offset: " + $"{resultDate:G}");
#else
                /*
                //the start of the day
                reinforceDay = reinforceDay.Subtract(reinforceDay.TimeOfDay);

                //and this is when the reinforced state will end in real -> at the user defined offset
                reinforceDay = reinforceDay.AddHours(ReinforceHandler._offsetHoursWithinDay);
                */

                reinforceDay = resultDate;
#endif

            }
        }

        private class VulnerableState : ReinforceStateBase
        {
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(10 + FastRandom.NextInt(0, 3)));
            private DateTime _vulnerableEnd; 

            public VulnerableState(PBSReinforceHandler<T> reinforceHandler) : base(reinforceHandler)
            {
            }

            public override void OnSave()
            {
                ReinforceHandler._pbsUnit.DynamicProperties.Update(k.vulnerable, 1);
                ReinforceHandler._pbsUnit.DynamicProperties.Update(k.vulnerableEnd, _vulnerableEnd);

                base.OnSave();

            }


            //already in this state
            public override void ToVulnerable() { }

            //only to rest
            public override void ToReinforce(Unit attacker) { }

            public override bool IsVulnerable => true;

            public override bool CanBeKilled
            {
                get { return true; }
            }
            
            public override void Enter()
            {
                var pbsUnitObject = ReinforceHandler._pbsUnit;

                pbsUnitObject.DynamicProperties.Set(k.vulnerable, 1);
                
                //unless the init has already set it
                if (_vulnerableEnd.Equals(default(DateTime)))
                {
                    //we calculate a new end time
                    _vulnerableEnd = DateTime.Now.AddMinutes(VULNERABLE_LENGTH_MINUTES);
                    pbsUnitObject.DynamicProperties.Update(k.vulnerableEnd, _vulnerableEnd);
                    pbsUnitObject.ApplyPvPEffect(TimeSpan.FromMinutes(VULNERABLE_LENGTH_MINUTES));
                    ReinforceHandler.SetNextReinforceCounterIncreaseFromNow(VULNERABLE_LENGTH_MINUTES);

                    var zone = pbsUnitObject.Zone;
                    if (zone != null)
                    {
                        ReinforceHandler.LogWrite(PBSLogType.vulnerableStart);
                    }
                    
                }
                else
                {
                    //server start, was init
                    Logger.Info("VULNERABLE STATE ENTER: skipping calculation, server start. " + pbsUnitObject.Eid + " " + pbsUnitObject.Name);

                    //somewhere in the future
                    if ( DateTime.Now < _vulnerableEnd)
                    {
                        var pvpEffectTime = _vulnerableEnd.Subtract(DateTime.Now);
                        pbsUnitObject.ApplyPvPEffect(pvpEffectTime);
                    }
                }

                pbsUnitObject.States.Reinforced = false;

               

                base.Enter();
            }

            public override void Exit()
            {
                var pbsUnitObject = ReinforceHandler._pbsUnit;

                pbsUnitObject.DynamicProperties.Update(k.vulnerable, 0);
                pbsUnitObject.DynamicProperties.Update(k.vulnerableEnd, default(DateTime));
                
                var zone = pbsUnitObject.Zone;
                if (zone != null)
                {
                    ReinforceHandler.StartAsyncLog(PBSLogType.vulnerableEnd);
                }

                base.Exit();
                
            }

            public override void Update(TimeSpan time)
            {
                base.Update(time);

                _timer.Update(time);
                if (!_timer.Passed)
                    return;

                ToNormal();

            }

            public override void AddToDictionary(IDictionary<string, object> info)
            {
                base.AddToDictionary(info);

                info.Add(k.vulnerable, 1);
                info.Add(k.vulnerableEnd,_vulnerableEnd);

            }

            public void Init(Unit owner)
            {
                if (owner.DynamicProperties.Contains(k.vulnerableEnd))
                {
                    var v = owner.DynamicProperties.GetOrAdd<DateTime>(k.vulnerableEnd);

                    if (!v.Equals(default(DateTime))  && v > DateTime.Now)
                    {
                        Logger.Info("vulnerable end was set by init. " + owner);
                        _vulnerableEnd = v;
                    }

                }
            }
        }



        public void Init()
        {

            IReinforceState state = new NormalState(this);

            _currentReinforceCounter = _pbsUnit.DynamicProperties.Contains(k.reinforceCounter) ? _pbsUnit.DynamicProperties.GetOrAdd<int>(k.reinforceCounter) : GetReinforceCounterMax();
            _nextReinforceCounterIncrease = _pbsUnit.DynamicProperties.Contains(k.nextReinforceIncrease) ? _pbsUnit.DynamicProperties.GetOrAdd<DateTime>(k.nextReinforceIncrease) : DateTime.Now;

            if (_pbsUnit.DynamicProperties.Contains(k.offsetWithinDay))
            {
                _offsetHoursWithinDay = _pbsUnit.DynamicProperties.GetOrAdd<int>(k.offsetWithinDay);
            }

            
            if (_pbsUnit.DynamicProperties.Contains(k.isReinforced))
            {
                var isReinforced = _pbsUnit.DynamicProperties.GetOrAdd<int>(k.isReinforced) == 1;

                if (isReinforced)
                {
                    Logger.Info("Reinforce active state was inited " + _pbsUnit);
                    state = new ActiveReinforceState(this, null);
                    ((ActiveReinforceState)state).Init(_pbsUnit);
                }
            }

            
            if (_pbsUnit.DynamicProperties.Contains(k.vulnerable))
            {
                var isVulnerable = _pbsUnit.DynamicProperties.GetOrAdd<int>(k.vulnerable) == 1;

                if (isVulnerable )
                {
                    //minden stimmel, akkor mehetunk egybol
                    Logger.Info("Vulnerable state was inited " + _pbsUnit);
                    state = new VulnerableState(this);
                    ((VulnerableState) state).Init(_pbsUnit);


                }
            }

            _fsm.ChangeState( state);


        }

        public void OnSave()
        {
            CurrentState.OnSave();

            

            

            _pbsUnit.DynamicProperties.Update(k.offsetWithinDay, _offsetHoursWithinDay);

            _pbsUnit.DynamicProperties.Update(k.reinforceCounter, _currentReinforceCounter);
            _pbsUnit.DynamicProperties.Update(k.nextReinforceIncrease, _nextReinforceCounterIncrease);

        }


        //called from a client request
        public void SetReinforceOffset(Character issuer, int offset)
        {
            CurrentState.IsReinforced.ThrowIfTrue(ErrorCodes.NotPossibleDuringReinforce);

            //sajat magamnak
            _offsetHoursWithinDay = offset.Clamp(0, 23);

            var networkNodes = _pbsUnit.ConnectionHandler.NetworkNodes.ToArray();

            foreach (var networkNode in networkNodes)
            {
                networkNode.ReinforceHandler.CurrentState.IsReinforced.ThrowIfTrue(ErrorCodes.NetworkHasReinforcedNode);
            }

            //spread it on full network, including myself
            foreach (var node in networkNodes)
            {
                node.ReinforceHandler.ForceDailyOffset(_offsetHoursWithinDay);
            }
        }


        //if (!IsReinforced && !IsVulnerable && _pbsUnit.GetPBSObjectHandler.IsFullyConstructed && !_pbsUnit.GetPBSObjectHandler.IsOrphaned && IsReinforcable)


        public void AddToDictionary(IDictionary<string, object> info)
        {
           
            _fsm.Current.AddToDictionary(info);

        }


#if DEBUG
      

        public const double REINFORCE_THRESHOLD = 0.5;
        public const int VULNERABLE_LENGTH_MINUTES = 5; //2 hours
        public const int REINFORCE_LENGTH_MINUTES = 6; //3 days
        public const int REINFORCE_COUNTER_INCREASE_MINUTES = 6; //2 days
       


#else
        //release values
        public const double REINFORCE_THRESHOLD = 0.5;
        public const int VULNERABLE_LENGTH_MINUTES = 120; //2 hours
        public const int REINFORCE_LENGTH_MINUTES = 4320; //3 days
        public const int REINFORCE_COUNTER_INCREASE_MINUTES = 2880; //2 days
        
#endif

    }
}