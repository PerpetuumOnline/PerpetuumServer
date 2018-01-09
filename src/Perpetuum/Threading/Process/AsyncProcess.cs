using System;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;

namespace Perpetuum.Threading.Process
{
    public class AsyncProcess : ProcessDecorator
    {
        private readonly CancellationTokenSource _cancellation;

        public AsyncProcess(IProcess process) : base(process)
        {
            _cancellation = new CancellationTokenSource();
        }

        public override void Stop()
        {
            _cancellation.Cancel();

            while (_running)
            {
                Thread.Sleep(1000);
            }

            base.Stop();
        }

        private TimeSpan _elapsed;
        private bool _running;

        public override void Update(TimeSpan time)
        {
            _elapsed += time;

            if (_running )
                return;

            _running = true;

            var elapsed = _elapsed;
            _elapsed = TimeSpan.Zero;

            Task.Run(() =>
            {
                try
                {
                    base.Update(elapsed);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
                finally
                {
                    _running = false;
                }
            },_cancellation.Token);
        }
    }
}