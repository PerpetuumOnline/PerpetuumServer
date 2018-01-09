using System;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;
using Perpetuum.Timers;

namespace Perpetuum.Zones.PBS
{
    //neki van cycletimeja
    //csinalja a pbsactiont


    /// <summary>
    /// This class is able to run an action periodically
    /// </summary>
    public abstract class PBSActiveObject : PBSObject
    {
        private readonly Lazy<int> _cycleTime ;
        

        private readonly IntervalTimer _updateInterval = new IntervalTimer(TimeSpan.FromSeconds(60)); //start with a long pause

        private int CycleTime
        {
            get { return _cycleTime.Value; }
        }

        protected PBSActiveObject() 
        {
            _cycleTime = new Lazy<int>(LazyInitCycleTime);
        }

        private int LazyInitCycleTime()
        {
            if (ED.Config.cycle_time != null)
                return (int) ED.Config.cycle_time;

            Logger.Error("consistency error. no cycle_time was defined for definition: " + Definition + " " + ED.Name);
            return (int) TimeSpan.FromSeconds(30).TotalMilliseconds;
        }

        private int _inProgress;

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            
            _updateInterval.Update(time);

            if (!_updateInterval.Passed)
                return;

            _updateInterval.Interval = TimeSpan.FromMilliseconds(CycleTime + FastRandom.NextInt(-2,2));
            _updateInterval.Reset();

            var onlineStatus = OnlineStatus;

            if (!this.IsFullyConstructed() || !onlineStatus)
                return;

            var zone = Zone;
            if (zone == null)
                return;

            if ( Interlocked.CompareExchange(ref _inProgress,1,0) == 1)
                return;
            
            Task.Run(() => { PBSActiveObjectAction(zone); }).ContinueWith(t => { Interlocked.Exchange(ref _inProgress, 0);}).LogExceptions();
        }

        protected virtual void PBSActiveObjectAction(IZone zone) { }
    }
}