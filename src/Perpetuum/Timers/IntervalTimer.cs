using System;

namespace Perpetuum.Timers
{
    /// <summary>
    /// Interval timer class
    /// </summary>
    public class IntervalTimer
    {
        private TimeSpan _interval;
        public TimeSpan Elapsed { get; private set; }

        public TimeSpan Interval
        {
            get { return _interval; }
            set
            {
                _interval = value;
                Elapsed = TimeSpan.Zero;
            }
        }

        public IntervalTimer(int interval,bool random = false) : this(TimeSpan.FromMilliseconds(interval),random)
        {
            
        }

        public IntervalTimer(TimeSpan interval,bool random = false)
        {
            Interval = interval;

            if (random)
            {
                Elapsed = TimeSpan.FromMilliseconds(FastRandom.NextDouble()*interval.TotalMilliseconds);
            }
        }

        public TimeSpan Remaining
        {
            get { return Interval - Elapsed; }
        }

        public bool Passed
        {
            get { return Elapsed >= Interval; }
        }

        public void Reset()
        {
            Elapsed = TimeSpan.Zero;
        }

        public IntervalTimer Update(TimeSpan time)
        {
            Elapsed += time;
            return this;
        }

        public void IsPassed(Action action)
        {
            IsPassed(e => action());
        }

        public void IsPassed(Action<TimeSpan> action)
        {
            if (!Passed)
                return;

            try
            {
                action(Elapsed);
            }
            finally
            {
                Reset();
            }
        }
    }
}