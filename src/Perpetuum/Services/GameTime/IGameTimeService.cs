using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Services.Daytime
{
    /// <summary>
    /// Service that tracks the in-game time as players experience on all zones
    /// </summary>
    public interface IGameTimeService : IObservable<GameTimeInfo>, IProcess
    {
        [NotNull]
        GameTimeInfo GetCurrentDayTime();
    }
}
