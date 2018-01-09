using System;
using System.Diagnostics;

namespace Perpetuum.Timers
{
    /// <summary>
    /// Global millisec timer class
    /// </summary>
    public static class GlobalTimer
    {
        private static readonly Stopwatch _timer = new Stopwatch();
       
        static GlobalTimer()
        {
            _timer.Start();
        }

        public static TimeSpan Elapsed
        {
            [DebuggerStepThrough]
            get { return _timer.Elapsed; }
        }

        public static bool IsPassed(ref TimeSpan last, TimeSpan interval)
        {
            if ((Elapsed - last) < interval)
                return false;

            last = Elapsed;
            return true;
        }
    }
   
}