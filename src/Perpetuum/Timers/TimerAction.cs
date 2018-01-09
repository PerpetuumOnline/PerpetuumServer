using System;
using System.Threading.Tasks;

namespace Perpetuum.Timers
{
    public class TimerAction
    {
        private readonly Action _action;
        private IntervalTimer _timer;
        private readonly bool _async;
        private bool _running;

        public TimerAction(Action action, int interval, bool async = false) : this(action, TimeSpan.FromMilliseconds(interval), async)
        {
        }

        public TimerAction(Action action, TimeSpan interval, bool async = false)
        {
            _action = action;
            _timer = new IntervalTimer(interval);
            _async = async;
        }

        public void Update(TimeSpan time)
        {
            _timer.Update(time);

            if ( !_timer.Passed )
                return;

            _timer.Reset();

            if (_async)
            {
                if (_running)
                    return;

                _running = true;
                Task.Run(() => _action()).ContinueWith(t => _running = false);
            }
            else
            {
                _action();
            }
        }

        public static TimerAction CreateWithRandom(Action action, int interval, bool async = false)
        {
            return CreateWithRandom(action, TimeSpan.FromMilliseconds(interval), async);
        }

        public static TimerAction CreateWithRandom(Action action, TimeSpan interval, bool async = false)
        {
            return new TimerAction(action, interval, async)
            {
                _timer = new IntervalTimer(interval,true)
            };
        }
    }
}