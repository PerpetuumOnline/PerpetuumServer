using Perpetuum.ExportedTypes;
using Perpetuum.Services.Daytime;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.Weather;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;
using System;
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
        private readonly IDictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, ZoneEffect> _effects = new Dictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, ZoneEffect>();

        public EnvironmentalEffectHandler(IZone zone)
        {
            _zone = zone;
            _effects = InitEffectCollection();
        }

        private IDictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, ZoneEffect> InitEffectCollection()
        {
            var dict = new Dictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, ZoneEffect>()
            {
                { Tuple.Create(GameTimeInfo.DayState.DAY, WeatherInfo.WeatherState.NEUTRAL_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_day, true) },
                { Tuple.Create(GameTimeInfo.DayState.DAY, WeatherInfo.WeatherState.GOOD_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_day_clear, true) },
                { Tuple.Create(GameTimeInfo.DayState.DAY, WeatherInfo.WeatherState.BAD_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_day_overcast, true) },
                { Tuple.Create(GameTimeInfo.DayState.NIGHT, WeatherInfo.WeatherState.NEUTRAL_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_night, true) },
                { Tuple.Create(GameTimeInfo.DayState.NIGHT, WeatherInfo.WeatherState.GOOD_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_night_clear, true) },
                { Tuple.Create(GameTimeInfo.DayState.NIGHT, WeatherInfo.WeatherState.BAD_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_night_overcast, true) },
                { Tuple.Create(GameTimeInfo.DayState.NEUTRAL, WeatherInfo.WeatherState.GOOD_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_weather_good, true) },
                { Tuple.Create(GameTimeInfo.DayState.NEUTRAL, WeatherInfo.WeatherState.BAD_WEATHER), new ZoneEffect(_zone.Id, EffectType.effect_weather_bad, true) },
                { Tuple.Create(GameTimeInfo.DayState.NEUTRAL, WeatherInfo.WeatherState.NEUTRAL_WEATHER), null }
            };
            return dict;
        }

        private ZoneEffect GetEffect(GameTimeInfo.DayState dayState, WeatherInfo.WeatherState weatherState)
        {
            var lookupEffect = Tuple.Create(dayState, weatherState);
            if (_effects.TryGetValue(lookupEffect, out ZoneEffect effect))
            {
                return effect;
            }
            return null;
        }

        private void OnStateChange()
        {
            if (_gameTime == null || _weatherState == null)
                return;

            var nextEffect = GetEffect(_gameTime.GetDayState(), _weatherState.getWeatherState());

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
