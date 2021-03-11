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
        private readonly IDictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, dynamic> _weatherDict = new Dictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, dynamic>();
        public EnvironmentalEffectHandler(IZone zone)
        {
            _zone = zone;
            _effects = InitEffectCollection(_effectTypes);
            _weatherDict = InitWeatherCollection();
        }

        private IDictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, dynamic> InitWeatherCollection()
        {
            var dict = new Dictionary<Tuple<GameTimeInfo.DayState, WeatherInfo.WeatherState>, dynamic>
            {
                { Tuple.Create(GameTimeInfo.DayState.DAY, WeatherInfo.WeatherState.NEUTRAL_WEATHER), EffectType.effect_day },
                { Tuple.Create(GameTimeInfo.DayState.DAY, WeatherInfo.WeatherState.GOOD_WEATHER), EffectType.effect_day_clear },
                { Tuple.Create(GameTimeInfo.DayState.DAY, WeatherInfo.WeatherState.BAD_WEATHER), EffectType.effect_day_overcast },
                { Tuple.Create(GameTimeInfo.DayState.NIGHT, WeatherInfo.WeatherState.NEUTRAL_WEATHER), EffectType.effect_night },
                { Tuple.Create(GameTimeInfo.DayState.NIGHT, WeatherInfo.WeatherState.GOOD_WEATHER), EffectType.effect_night_clear },
                { Tuple.Create(GameTimeInfo.DayState.NIGHT, WeatherInfo.WeatherState.BAD_WEATHER), EffectType.effect_night_overcast },
                { Tuple.Create(GameTimeInfo.DayState.NEUTRAL, WeatherInfo.WeatherState.GOOD_WEATHER), EffectType.effect_weather_good },
                { Tuple.Create(GameTimeInfo.DayState.NEUTRAL, WeatherInfo.WeatherState.BAD_WEATHER), EffectType.effect_weather_bad },
                { Tuple.Create(GameTimeInfo.DayState.NEUTRAL, WeatherInfo.WeatherState.NEUTRAL_WEATHER), null }
            };
            return dict;
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

            var weatherResult = Tuple.Create(_gameTime.GetDayState(), _weatherState.getWeatherState());
            nextEffect = GetEffect(_weatherDict[weatherResult]);          

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
