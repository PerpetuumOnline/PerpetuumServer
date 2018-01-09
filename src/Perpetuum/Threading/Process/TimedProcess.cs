using System;
using Perpetuum.Log;

namespace Perpetuum.Threading.Process
{
    public class ProcessDecorator : IProcess
    {
        private readonly IProcess _process;

        public ProcessDecorator(IProcess process)
        {
            _process = process;
        }

        public IProcess InnerProcess => _process;

        public virtual void Start()
        {
            _process.Start();
        }

        public virtual void Stop()
        {
            _process.Stop();
        }

        public virtual void Update(TimeSpan time)
        {
            _process.Update(time);
        }
    }

    public class TimedProcess : ProcessDecorator
    {
        private readonly TimeSpan _interval;

        public TimedProcess(IProcess process,TimeSpan interval) : base(process)
        {
            _interval = interval;
        }

        private TimeSpan _elapsed;

        public override void Update(TimeSpan time)
        {
            _elapsed += time;

            if ( _elapsed < _interval )
                return;

            _elapsed -= _interval;

            try
            {
                base.Update(_interval);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                throw;
            }
        }
    }
}