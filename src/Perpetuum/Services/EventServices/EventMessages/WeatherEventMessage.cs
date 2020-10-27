using Perpetuum.Services.Weather;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class WeatherEventMessage : EventMessage
    {
        public WeatherInfo Weather { get; private set; }
        public int ZoneId { get; private set; }
        public WeatherEventMessage(WeatherInfo weather, int zoneId)
        {
            Weather = weather;
            ZoneId = zoneId;
        }
    }
}
