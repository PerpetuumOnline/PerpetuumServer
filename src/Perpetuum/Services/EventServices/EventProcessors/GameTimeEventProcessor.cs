using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.Daytime;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Effects.ZoneEffects;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// GameTimeMessage processor to modify ZoneEffects on a zone
    /// </summary>
    public class GameTimeEventProcessor : EventProcessor<EventMessage>
    {
        private readonly IZone _zone;
        private readonly Lazy<ZoneEffect> _dayEffect;
        private readonly Lazy<ZoneEffect> _nightEffect;
        public GameTimeEventProcessor(IZone zone)
        {
            _zone = zone;
            _dayEffect = new Lazy<ZoneEffect>(CreateDayEffect);
            _nightEffect = new Lazy<ZoneEffect>(CreateNightEffect);
        }

        private ZoneEffect CreateDayEffect()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_daytime_day, true);
        }

        private ZoneEffect CreateNightEffect()
        {
            return new ZoneEffect(_zone.Id, EffectType.effect_daytime_night, true);
        }

        public override void OnNext(EventMessage value)
        {
            if (value is GameTimeMessage msg)
            {
                if (msg.TimeInfo.IsNight)
                {
                    _zone.ZoneEffectHandler.AddEffect(_nightEffect.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_nightEffect.Value);
                }

                if (msg.TimeInfo.IsDay)
                {
                    _zone.ZoneEffectHandler.AddEffect(_dayEffect.Value);
                }
                else
                {
                    _zone.ZoneEffectHandler.RemoveEffect(_dayEffect.Value);
                }
            }
        }
    }
}

