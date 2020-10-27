using System;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.Weather
{
    /// <summary>
    /// Service that generates weather state-transitions
    /// </summary>
    public interface IWeatherService : IObservable<WeatherInfo>, IProcess
    {
        [NotNull]
        WeatherInfo GetCurrentWeather();
        void SetCurrentWeather(WeatherInfo weather);
    }
}