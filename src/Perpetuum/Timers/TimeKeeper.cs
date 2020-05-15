using System;

namespace Perpetuum.Timers
{
    /// <summary>
    /// TimeTracker that uses internal timestamp to measure time passage.
    /// Update is no-op, and properties all compute time differences on-call.
    /// </summary>
    public class TimeKeeper : TimeTracker
    {
        private DateTime _start;
        public TimeKeeper(TimeSpan duration) : base(duration)
        {
            Elapsed = TimeSpan.Zero;
            Duration = duration;
            Start();
        }

        /// <summary>
        /// Sets NOW as the start time, use when object creation is not the desired start time
        /// </summary>
        public void Start()
        {
            SetStart(DateTime.UtcNow);
        }

        /// <summary>
        /// Allow arbitrary starting timestamps
        /// </summary>
        /// <param name="time">UTC time from which to start</param>
        public void SetStart(DateTime time)
        {
            _start = time;
        }

        /// <summary>
        /// No-op override of TimeTracker Update
        /// </summary>
        /// <param name="time"></param>
        public override void Update(TimeSpan time) { }

        /// <summary>
        /// Dynamically computes Elapsed time as difference from UTC now to the start stamp
        /// </summary>
        public override TimeSpan Elapsed
        {
            get { return DateTime.UtcNow - _start; }
        }

        public override void Reset()
        {
            Start();
        }
    }
}