using System;
using Perpetuum.Services.Sessions;

namespace Perpetuum.Services.Relay
{
    public interface IRelayStateService
    {
        RelayState State { get; set; }
        event Action<RelayState> StateChanged;
        void SendStateToClient(ISession session);
        void ConfigOnlyAllowAdmins(bool enabled);
    }
}