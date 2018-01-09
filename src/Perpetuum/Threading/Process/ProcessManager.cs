using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Perpetuum.Log;
using Perpetuum.Timers;

namespace Perpetuum.Threading.Process
{
    [UsedImplicitly]
    public class ProcessManager : IProcessManager
    {
        private readonly TimeSpan _updateInterval;
        private readonly Thread _thread;
        private ImmutableList<IProcess> _processes = ImmutableList<IProcess>.Empty;
        private bool _isRunning;

        public ProcessManager(TimeSpan updateInterval)
        {
            _updateInterval = updateInterval;
            _thread = new Thread(UpdateLoop) { Name = "MainLoop", IsBackground = true };
        }

        public void AddProcess(IProcess process)
        {
            ImmutableInterlocked.Update(ref _processes,p => p.Add(process));
        }

        public void RemoveProcess(IProcess process)
        {
            ImmutableInterlocked.Update(ref _processes, p => p.Remove(process));
            StopProcess(process);
        }

        public IEnumerable<IProcess> Processes => _processes;

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;

            foreach (var process in _processes)
            {
                process.Start();
            }

            _thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;

            if (_thread.IsAlive)
            {
                if (!_thread.Join(TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        _thread.Abort();
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }
            }

            foreach (var process in _processes)
            {
                StopProcess(process);
            }
        }

        private static void StopProcess(IProcess process)
        {
            try
            {
                process.Stop();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        private void UpdateLoop()
        {
            var last = GlobalTimer.Elapsed;
            var prevSleepTime = TimeSpan.Zero;

            while (_isRunning)
            {
                var now = GlobalTimer.Elapsed;
                var elapsed = now - last;
                last = now;

                try
                {
                    foreach (var process in _processes)
                    {
                        process.Update(elapsed);
                    }
                }
                catch (ThreadAbortException)
                {
                    _isRunning = false;
                    Thread.ResetAbort();
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }

                if (elapsed <= _updateInterval + prevSleepTime)
                {
                    prevSleepTime = _updateInterval + prevSleepTime - elapsed;
                    Thread.Sleep(prevSleepTime);
                }
                else
                    prevSleepTime = TimeSpan.Zero;
            }
        }
    }
}