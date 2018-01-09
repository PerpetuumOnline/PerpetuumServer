using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Timers;
using Perpetuum.Zones;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{

    /// <summary>
    /// 
    /// Uses an alarm perios from options, or falls back to default value
    /// Starts a timer for a player
    /// The player has to stay within a range for the period, it checks it in every 2 seconds
    /// Informs the client if the period is over 
    /// Finishes target
    /// 
    /// </summary>
    public class AlarmSwitch : MissionSwitch
    {
        private static readonly TimeSpan _alarmPeriod = TimeSpan.FromSeconds(5);
        private ImmutableHashSet<RegisteredPlayer> _registeredPlayers = ImmutableHashSet<RegisteredPlayer>.Empty;

        public AlarmSwitch() : this(MissionTargetType.use_switch)
        {

        }

        protected AlarmSwitch(MissionTargetType targetType) : base(targetType)
        {

        }


        /*
        public override Dictionary<string, object> GetUseResult()
        {
            var result = base.GetUseResult();

            result.Add(k.timeOut, (int)_alarmPeriod.TotalMilliseconds);
            result.Add(k.started, (long)_elapsed.TotalMilliseconds);
            result.Add(k.now, (long)_elapsed.TotalMilliseconds);

            return result;
        }*/


        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void Use(Player player)
        {
            CanUseAndCheckError(player);

            var x = _registeredPlayers.FirstOrDefault(r => r.player == player);

            x.ThrowIfNotNull(ErrorCodes.MissionAlarmAlreadyStarted, gex =>
            {

               /*
               gex.SetData( k.timeOut, (int) AlarmPeriod.TotalMilliseconds);
               gex.SetData( k.started, registeredPlayer.registerTime);
               gex.SetData( k.now, (long)GlobalTimer.Elapsed.TotalMilliseconds);
               */

            });     

            var rp = new RegisteredPlayer(player,_alarmPeriod);

            ImmutableInterlocked.Update(ref _registeredPlayers, p =>
            {
                return p.Add(rp);
            });

            CreateInteractionBeam(player);

            player.SendStartProgressBar(this,_alarmPeriod,_elapsed);
        }
        
        private readonly IntervalTimer _alarmTimer = new IntervalTimer(TimeSpan.FromSeconds(2));
        
        private TimeSpan _elapsed;

        protected override void OnUpdate(TimeSpan time)
        {
            _elapsed += time;

            _alarmTimer.Update(time);

            if (_alarmTimer.Passed)
            {
                var r = new List<RegisteredPlayer>();

                try
                {
                    foreach (var registeredPlayer in _registeredPlayers)
                    {
                        registeredPlayer.Update(_alarmTimer.Elapsed);

                        if (registeredPlayer.Timer.Expired)
                        {
                            r.Add(registeredPlayer);
                            OnPeriodOver(registeredPlayer.player);
                            continue;
                        }

                        if (!registeredPlayer.IsInAlarmSwitchRange(this))
                        {
                            registeredPlayer.player.SendEndProgressBar(this, false);
                            r.Add(registeredPlayer);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
                finally
                {
                    _alarmTimer.Reset();

                    if (r.Count > 0)
                    {
                        ImmutableInterlocked.Update(ref _registeredPlayers, rp =>
                        {
                            var b = rp.ToBuilder();

                            foreach (var registeredPlayer in r)
                            {
                                b.Remove(registeredPlayer);
                            }

                            return b.ToImmutable();
                        });
                    }
                }
            }

            base.OnUpdate(time);
        }

        

        
        protected virtual void OnPeriodOver([NotNull]Player player)
        {
            player.MissionHandler.EnqueueMissionEventInfo(new SwitchEventInfo(player,this, CurrentPosition));
            
            CreateSuccessBeam(player);

            var info = BaseInfoToDictionary();
            info.Add(k.success, true);
            Message.Builder.SetCommand(Commands.AlarmOver).WithData(info).ToCharacter(player.Character).Send();
        }

        protected class RegisteredPlayer
        {
            [NotNull]
            public readonly Player player;

            private readonly TimeTracker _timer;

            public RegisteredPlayer(Player player, TimeSpan time)
            {
                this.player = player;
                _timer = new TimeTracker(time);
                
            }
           
            public void Update(TimeSpan elapsed)
            {
                _timer.Update(elapsed);
                
            }

            public TimeTracker Timer
            {
                get { return _timer; }
            }

            public bool IsInAlarmSwitchRange(AlarmSwitch alarmSwitch)
            {
                //not passed yet, is he standing nearby?
                return alarmSwitch.IsInRangeOf3D(player, DistanceConstants.ALARMSWITCH_STAYCLOSE_DISTANCE);
            }
           
        }
    }


}
