using Perpetuum.Reactive;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using System;

namespace Perpetuum.Services.Weather
{
    /// <summary>
    /// Weather observer for transmitting EventMessages to EventListenerService
    /// </summary>
    public class WeatherEventListener : Observer<WeatherInfo>
    {
        private readonly IZone _zone;
        private readonly EventListenerService _listener;
        public WeatherEventListener(EventListenerService listener, IZone zone)
        {
            _zone = zone;
            _listener = listener;
        }

        public override void OnNext(WeatherInfo info)
        {
            _listener.PublishMessage(new WeatherEventMessage(info, _zone.Id));
        }
    }

    /// <summary>
    /// Generic weather observer that invokes the provided action on weather events
    /// </summary>
    public class WeatherMonitor : Observer<WeatherInfo>
    {
        private readonly Action<WeatherInfo> _onNext;
        public WeatherMonitor(Action<WeatherInfo> onNext)
        {
            _onNext = onNext;
        }

        public override void OnNext(WeatherInfo info)
        {
            _onNext(info);
        }
    }
}
