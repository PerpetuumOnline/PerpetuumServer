using System;
using Perpetuum.Threading.Process;

namespace Perpetuum.Zones
{
    public interface IWeatherService : IObservable<Packet>,IProcess
    {
        [NotNull]
        WeatherInfo GetCurrentWeather();
    }
}