using Perpetuum.ExportedTypes;
using Perpetuum.Services.Daytime;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.Weather;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;
using System.Collections.Generic;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// Environment Event processor to modify ZoneEffects on a zone based on weather and time of day
    /// </summary>
    public class EnvironmentalEffectHandler : EventProcessor
    {
        private readonly IZone _zone;
        private WeatherInfo _weatherState;
        private GameTimeInfo _gameTime;
        private ZoneEffect _currentEffect;
        private readonly EffectType[] _effectTypes = new EffectType[] {
            EffectType.effect_day,
            EffectType.effect_day_clear,
            EffectType.effect_day_overcast,
            EffectType.effect_night,
            EffectType.effect_night_clear,
            EffectType.effect_night_overcast,
            EffectType.effect_weather_good,
            EffectType.effect_weather_bad
        };
        private readonly IDictionary<EffectType, ZoneEffect> _effects = new Dictionary<EffectType, ZoneEffect>();
        public EnvironmentalEffectHandler(IZone zone)
        {
            _zone = zone;
            _effects = InitEffectCollection(_effectTypes);
        }

        private IDictionary<EffectType, ZoneEffect> InitEffectCollection(EffectType[] effectTypes)
        {
            var dict = new Dictionary<EffectType, ZoneEffect>();
            foreach (var effType in effectTypes)
            {
                dict.Add(effType, new ZoneEffect(_zone.Id, effType, true));
            }
            return dict;
        }

        private ZoneEffect GetEffect(EffectType type)
        {
            if (_effects.TryGetValue(type, out ZoneEffect effect))
            {
                return effect;
            }
            return null;
        }

        private void OnStateChange()
        {
            if (_gameTime == null || _weatherState == null)
                return;

            ZoneEffect nextEffect = null;
            if (_gameTime.IsDay)
            {
                if (_weatherState.IsGoodWeather)
                {
                    nextEffect = GetEffect(EffectType.effect_day_clear);
                }
                else if (_weatherState.IsBadWeather)
                {
                    nextEffect = GetEffect(EffectType.effect_day_overcast);
                }
                else
                {
                    nextEffect = GetEffect(EffectType.effect_day);
                }
            }
            else if (_gameTime.IsNight)
            {
                if (_weatherState.IsGoodWeather)
                {
                    nextEffect = GetEffect(EffectType.effect_night_clear);
                }
                else if (_weatherState.IsBadWeather)
                {
                    nextEffect = GetEffect(EffectType.effect_night_overcast);
                }
                else
                {
                    nextEffect = GetEffect(EffectType.effect_night);
                }
            }
            else
            {
                if (_weatherState.IsGoodWeather)
                {
                    nextEffect = GetEffect(EffectType.effect_weather_good);
                }
                else if (_weatherState.IsBadWeather)
                {
                    nextEffect = GetEffect(EffectType.effect_weather_bad);
                }
            }

            var isSameEffect = ReferenceEquals(_currentEffect, nextEffect) ||
                (_currentEffect != null && _currentEffect.Equals(nextEffect));

            if (!isSameEffect)
            {
                _zone.ZoneEffectHandler.RemoveEffect(_currentEffect);
                _zone.ZoneEffectHandler.AddEffect(nextEffect);
                _currentEffect = nextEffect;
            }
        }

        private bool TryGetWeatherMessage(EventMessage value)
        {
            var msg = value as WeatherEventMessage;
            var isValidMsg = msg != null && msg.ZoneId == _zone.Id;
            if (isValidMsg)
            {
                _weatherState = msg.Weather;
            }
            return isValidMsg;
        }

        private bool TryGetGameTimeMessage(EventMessage value)
        {
            var msg = value as GameTimeMessage;
            var isValidMsg = msg != null;
            if (isValidMsg)
            {
                _gameTime = msg.TimeInfo;
            }
            return isValidMsg;
        }

        public override void HandleMessage(EventMessage value)
        {
            var stateChange = TryGetWeatherMessage(value) || TryGetGameTimeMessage(value);
            if (stateChange)
            {
                OnStateChange();
            }
        }
    }
}
