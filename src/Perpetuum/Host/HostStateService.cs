using System;
using Perpetuum.Log;

namespace Perpetuum.Host
{
    public class HostStateService : IHostStateService
    {
        private HostState _state;

        public HostState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state == value)
                    return;

                _state = value;
                OnStateChanged(value);
            }
        }

        public event Action<IHostStateService,HostState> StateChanged;

        private void OnStateChanged(HostState state)
        {
            Logger.Info($">>>> Perpetuum Server State : [{_state}]");
            try
            {
                StateChanged?.Invoke(this,state);
            }
            catch(Exception ex)
            {
                Logger.Exception(ex);
            }
        }
    }
}