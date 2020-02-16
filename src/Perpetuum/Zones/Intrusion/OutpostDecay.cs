using System;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Outpost Decay handler - updates outpost stability with a system-generated SAP event
    /// </summary>
    public class OutpostDecay
    {
        private readonly EventListenerService _eventChannel;
        private readonly static TimeSpan noDecayBefore = TimeSpan.FromDays(3);
        private readonly static TimeSpan decayRate = TimeSpan.FromDays(1);
        private TimeSpan timeSinceLastDecay = TimeSpan.Zero;
        private TimeSpan lastSuccessfulIntrusion = TimeSpan.Zero;
        private readonly static int decayPts = -5;
        private StabilityAffectingEvent.StabilityAffectBuilder _builder;

        public OutpostDecay(EventListenerService eventChannel, Outpost outpost)
        {
            _eventChannel = eventChannel;
            var def = EntityDefault.GetByName("def_outpost_decay");
             _builder = StabilityAffectingEvent.Builder()
                .WithOutpost(outpost)
                .WithSapDefinition(def.Definition)
                .WithPoints(decayPts);
        }

        public void OnUpdate(TimeSpan time)
        {
            lastSuccessfulIntrusion += time;
            if (lastSuccessfulIntrusion < noDecayBefore)
                return;

            timeSinceLastDecay += time;
            if (timeSinceLastDecay > decayRate)
            {
                timeSinceLastDecay = TimeSpan.Zero;
                DoDecay();
            }
        }

        /// <summary>
        /// Called when a SAP is completed
        /// </summary>
        public void ResetDecayTimer()
        {
            lastSuccessfulIntrusion = TimeSpan.Zero;
        }

        private void DoDecay()
        {
            _eventChannel.PublishMessage(_builder.Build());
        }
    }
}
