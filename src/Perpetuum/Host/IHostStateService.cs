using System;

namespace Perpetuum.Host
{
    public interface IHostStateService
    {
        HostState State { get; set; }
        event Action<IHostStateService,HostState> StateChanged;
    }
}