using System;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzonePresence : DynamicPresence
    {
        public InterzonePresence(IZone zone, IPresenceConfiguration configuration) : base(zone, configuration)
        {
            if (Configuration.DynamicLifeTime != null)
                LifeTime = TimeSpan.FromSeconds((int)Configuration.DynamicLifeTime);
        }
    }
}