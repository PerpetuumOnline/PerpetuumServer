using Perpetuum.Services.Weather;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class WeatherEventMessage : IEventMessage
    {
        public EventType Type => EventType.Environmental;
        public WeatherInfo Weather { get; private set; }
        public int ZoneId { get; private set; }
        public WeatherEventMessage(WeatherInfo weather, int zoneId)
        {
            Weather = weather;
            ZoneId = zoneId;
        }
    }
}
