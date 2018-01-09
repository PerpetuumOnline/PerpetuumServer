using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;

namespace Perpetuum.Host
{
    public class HostShutDownManager 
    {
        private readonly IHostStateService _stateService;
        private Task _shutdownTask;
        private CancellationTokenSource _cancellation;

        private DateTime _shutDownTime;
        private bool _isInShutDown;
        private string _message;

        public HostShutDownManager(IHostStateService stateService)
        {
            _stateService = stateService;
        }

        public void StartShutDown(Command command,string message,DateTime time)
        {
            _isInShutDown = true;
            _message = message;
            CreateShutdownTask(time - DateTime.Now);
            //send all clients the start here
            SendToAll(command);
        }

        public void StopShutDown(Command command)
        {
            _isInShutDown = false;
            _shutDownTime = default(DateTime);

            //send all clients the cancellation
            SendToAll(command);
        }

        private void SendToAll(Command command)
        {
            Message.Builder.SetCommand(command).WithData(StateToDictionary()).ToAll().Send();
        }

        public Dictionary<string, object> StateToDictionary()
        {
            var result = new Dictionary<string, object>
                {
                    {k.date, _shutDownTime},
                    {k.active, _isInShutDown},
                    {k.message, _message}
                };
            return result;
        }

        private void CreateShutdownTask(TimeSpan delay)
        {
            CancelShutdown();

            _cancellation = new CancellationTokenSource();

            _shutDownTime = DateTime.Now.Add(delay);

            _shutdownTask = Task.Delay(delay, _cancellation.Token).ContinueWith(t =>
            {
                Logger.Info("--------------------------------");
                Logger.Info("");
                Logger.Info("      auto shutdown             ");
                Logger.Info("");
                Logger.Info("--------------------------------");
                _stateService.State = HostState.Stopping;
            }, _cancellation.Token);
        }

        private void CancelShutdown()
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
                _cancellation.Token.WaitHandle.WaitOne();

                _cancellation.Dispose();
                _cancellation = null;

                Logger.Info("--------------------------------");
                Logger.Info("");
                Logger.Info("      shutdown cancelled         ");
                Logger.Info("");
                Logger.Info("--------------------------------");
            }

            _shutdownTask = null;
            _shutDownTime = DateTime.MinValue;
        }

        private bool ShutdownTaskRunning => _shutdownTask?.Status == TaskStatus.WaitingForActivation;

        private void StartShutdown(TimeSpan delay)
        {
            CreateShutdownTask(delay);

            Logger.Info("--------------------------------");
            Logger.Info("");
            Logger.Info($"      dev restart requested ({(int)delay.TotalSeconds}s)");
            Logger.Info("");
            Logger.Info("--------------------------------");
        }

        public void Shutdown(TimeSpan delay)
        {
            StartShutdown(delay);
            SendStateToAll(Commands.ServerShutDown);
        }

        private void SendStateToAll(Command command)
        {
            var result = new Dictionary<string, object>
                {
                    {k.date, _shutDownTime},
                    {k.active, ShutdownTaskRunning},
                };

            Message.Builder.SetCommand(command).WithData(result).ToAll().Send();
        }
    }
}
