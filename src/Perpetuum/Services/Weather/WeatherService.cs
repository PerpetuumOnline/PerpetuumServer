using System;
using Perpetuum.Reactive;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.Weather
{
    public class WeatherService : Process, IWeatherService
    {
        private static readonly byte[] _weatherLookUp = new byte[320];

        private readonly TimeRange _updateInterval;
        private readonly Observable<WeatherInfo> _observable;

        private WeatherInfo _current;

        static WeatherService()
        {
            //the full cycle
            for (var i = 0; i < _weatherLookUp.Length; i++)
            {
                _weatherLookUp[i] = (byte)i.Min(255);
            }
        }

        public WeatherService(TimeRange updateInterval)
        {
            _updateInterval = updateInterval;
            _current = GetNextWeather();
            _observable = new AnonymousObservable<WeatherInfo>(OnSubscribe);
        }

        private void OnSubscribe(IObserver<WeatherInfo> observer)
        {
            observer.OnNext(_current);
        }

        private WeatherInfo GetNextWeather()
        {
            var duration = FastRandom.NextTimeSpan(_updateInterval);

            return _current == null ? new WeatherInfo(GetRandomWeather(), GetRandomWeather(), duration) :
                new WeatherInfo(_current.Next, GetRandomWeather(), duration);
        }

        private int GetRandomWeather()
        {
            return _weatherLookUp.RandomElement();
        }

        public override void Update(TimeSpan time)
        {
            if (!_current.Update(time))
                return;

            _current = GetNextWeather();
            SendWeatherUpdate(_current);
        }

        public IDisposable Subscribe(IObserver<WeatherInfo> observer)
        {
            return _observable.Subscribe(observer);
        }

        public WeatherInfo GetCurrentWeather()
        {
            return _current;
        }

        private void SendWeatherUpdate(WeatherInfo info)
        {
            _observable.OnNext(info);
        }

        public void SetCurrentWeather(WeatherInfo weather)
        {
            _current = weather;
            SendWeatherUpdate(_current);
        }
    }
}