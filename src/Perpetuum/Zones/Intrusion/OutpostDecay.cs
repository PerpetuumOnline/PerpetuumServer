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
        private readonly Outpost _outpost;
        private readonly EventListenerService _eventChannel;
        private readonly static TimeSpan noDecayBefore = TimeSpan.FromDays(3);
        private readonly static TimeSpan decayRate = TimeSpan.FromDays(1);
        private TimeSpan timeSinceLastDecay = TimeSpan.Zero;
        private TimeSpan lastSuccessfulIntrusion = TimeSpan.Zero;
        private readonly static int decayPts = -5;
        private readonly EntityDefault def = EntityDefault.GetByName("def_outpost_decay");

        public OutpostDecay(EventListenerService eventChannel, Outpost outpost)
        {
            _outpost = outpost;
            _eventChannel = eventChannel;
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
        public void OnSAP()
        {
            lastSuccessfulIntrusion = TimeSpan.Zero;
        }

        private void DoDecay()
        {
            _eventChannel.PublishMessage(new StabilityAffectingEvent(_outpost, null, def.Definition, null, decayPts));
        }
    }
}
