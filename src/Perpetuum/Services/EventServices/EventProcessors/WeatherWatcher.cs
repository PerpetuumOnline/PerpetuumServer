using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;
using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// WeatherEvent processor to modify ZoneEffects on a zone
    /// </summary>
    public class WeatherWatcher : EventProcessor<EventMessage>
    {
        private readonly IZone _zone;
        private readonly Lazy<ZoneEffect> _goodWeather;
        private readonly Lazy<ZoneEffect> _badWeather;
        public WeatherWatcher(IZone zone)
        {
            _zone = zone;
            _goodWeather = new Lazy<ZoneEffect>(CreateGoodWeather);
            _badWeather = new Lazy<ZoneEffect>(CreateBadWeather);
        }

        private ZoneEffect CreateGoodWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_weather_good, true);
        }

        private ZoneEffect CreateBadWeather()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_weather_bad, true);
        }

        public override void OnNext(EventMessage value)
        {
            if (value is WeatherEventMessage msg && msg.ZoneId == _zone.Id)
            {
                if (msg.Weather.IsBadWeather)
                {
                    _zone.ZoneEffectHandler.AddEffect(_badWeather.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_badWeather.Value);
                }

                if (msg.Weather.IsGoodWeather)
                {
                    _zone.ZoneEffectHandler.AddEffect(_goodWeather.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_goodWeather.Value);
                }
            }
        }
    }
}
