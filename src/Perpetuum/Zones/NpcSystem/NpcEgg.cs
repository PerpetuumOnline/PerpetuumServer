using System;
using Perpetuum.Players;
using Perpetuum.Zones.Eggs;

namespace Perpetuum.Zones.NpcSystem
{
    public class NpcEgg : Egg
    {
        public override void Initialize()
        {
            DespawnTime = TimeSpan.FromMinutes(20);
            base.Initialize();
        }

        protected override void OnSummonSuccess(IZone zone, Player[] summoners)
        {
        }
    }
}