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
        private readonly IDictionary<Tuple<string, string>, EffectType> _weatherDict = new Dictionary<Tuple<string, string>, EffectType>();
        public EnvironmentalEffectHandler(IZone zone)
        {
            _zone = zone;
            _effects = InitEffectCollection(_effectTypes);
            _weatherDict = InitWeatherCollection();
        }

        private IDictionary<Tuple<string, string>, EffectType> InitWeatherCollection()
        {
            var dict = new Dictionary<Tuple<string, string>, EffectType>
            {
                { Tuple.Create("day", "neutral"), EffectType.effect_day },
                { Tuple.Create("day", "good"), EffectType.effect_day_clear },
                { Tuple.Create("day", "bad"), EffectType.effect_day_overcast },
                { Tuple.Create("night", "neutral"), EffectType.effect_night },
                { Tuple.Create("night", "good"), EffectType.effect_night_clear },
                { Tuple.Create("night", "bad"), EffectType.effect_night_overcast },
                { Tuple.Create("neutral", "good"), EffectType.effect_weather_good },
                { Tuple.Create("neutral", "bad"), EffectType.effect_weather_bad }
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
            string timeOfDay = "neutral";
            string weatherType = "neutral";

            if(_gameTime.IsDay)
            {
                timeOfDay = "day";
            }
            else if(_gameTime.IsNight)
            {
                timeOfDay = "night";
            }

            if(_weatherState.IsGoodWeather)
            {
                weatherType = "good";
            }
            else if(_weatherState.IsBadWeather)
            {
                weatherType = "bad";
            }

            //In cases where timeOfDay is supposed to stay "neutral", weatherType cannot also be "neutral" if we are to follow the logic this change is replacing.
            if (String.Equals(timeOfDay, weatherType))
            {
                return;
            }
            var weatherResult = Tuple.Create(timeOfDay, weatherType);
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
