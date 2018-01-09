using System;
using Perpetuum.Timers;

namespace Perpetuum
{
    public class Profiler
    {
        public static TimeSpan ExecutionTimeOf(int count, Action action)
        {
            return ExecutionTimeOf(() =>
            {
                for (var i = 0; i < count; i++)
                {
                    action();
                }
            });
        }

        public static TimeSpan ExecutionTimeOf(Action action)
        {
            TimeSpan elapsed;
            var then = GlobalTimer.Elapsed;
            try
            {
                action();
            }
            finally
            {
                elapsed = GlobalTimer.Elapsed - then;
            }

            return elapsed;
        }

        public static Action<TimeSpan> CreateUpdateProfiler(TimeSpan interval,Action<TimeSpan> action)
        {
            var timer = new IntervalTimer(interval);
            var updates = 0;

            return (e) =>
            {
                updates++;
                timer.Update(e);
                if (!timer.Passed)
                    return;

                try
                {
                    action(timer.Elapsed.Divide(updates));
                }
                finally
                {
                    updates = 0;
                    timer.Reset();
                }
            };
        }
    }
}