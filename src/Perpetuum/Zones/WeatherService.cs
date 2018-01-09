using System;
using Perpetuum.Reactive;
using Perpetuum.Threading.Process;

namespace Perpetuum.Zones
{
    public class WeatherService : Process, IWeatherService
    {
        private static readonly byte[] _weatherLookUp = new byte[320];

        private readonly TimeRange _updateInterval;
        private readonly Observable<Packet> _observable;

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
            _observable = AnonymousObservable<Packet>.Create(OnSubscribe);
        }

        private void OnSubscribe(IObserver<Packet> observer)
        {
            observer.OnNext(_current.CreateUpdatePacket());
        }

        private WeatherInfo GetNextWeather()
        {
            var duration = FastRandom.NextTimeSpan(_updateInterval);

            return _current == null ? new WeatherInfo(GetRandomWeather(),GetRandomWeather(), duration) : 
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
            var packet = _current.CreateUpdatePacket();
            _observable.OnNext(packet);
        }

        public IDisposable Subscribe(IObserver<Packet> observer)
        {
            return _observable.Subscribe(observer);
        }

        public WeatherInfo GetCurrentWeather()
        {
            return _current;
        }
    }
}