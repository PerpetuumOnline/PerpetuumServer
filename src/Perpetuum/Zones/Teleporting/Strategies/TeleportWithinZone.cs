using System;
using System.Threading.Tasks;
using Perpetuum.Players;
using Perpetuum.Zones.Finders.PositionFinders;

namespace Perpetuum.Zones.Teleporting.Strategies
{
    public class TeleportWithinZone : ITeleportStrategy
    {
        public delegate TeleportWithinZone Factory();

        public TeleportWithinZone()
        {
            ApplyInvulnerable = true;
            ApplyTeleportSickness = true;
            TeleportDelay = TimeSpan.FromSeconds(5);
        }

        public Position TargetPosition { get; set; }

        public TimeSpan TeleportDelay { private get; set; }

        public bool ApplyInvulnerable { private get; set; }
        public bool ApplyTeleportSickness { private get; set; }

        [CanBeNull]
        public Task DoTeleportAsync(Player player)
        {
            var zone = player.Zone;
            if (zone == null)
                return null;

            var finder = new ClosestWalkablePositionFinder(zone, TargetPosition);
            if (!finder.Find(out Position validPosition))
                return null;

            player.StopAllModules();

            if (ApplyInvulnerable)
            {
                // teleport kezdetenel rakjuk fel
                player.ApplyInvulnerableEffect();
            }

            // nem mozog, fade in
            player.States.InMoveable = true;
            player.States.LocalTeleport = true;

            var task = Task.Delay(TeleportDelay).ContinueWith(t =>
            {
                player.RemoveFromZone();
                player.CurrentSpeed = 0.0;
                player.AddToZone(zone,validPosition,ZoneEnterType.LocalTeleport);
                player.SendInitSelf();

                if (ApplyTeleportSickness)
                {
                    player.ApplyTeleportSicknessEffect();
                }
                if (ApplyInvulnerable)
                {
                    player.RemoveInvulnerableEffect(); // remove previous effect.
                    player.ApplyInvulnerableEffect(); // re-add effect to restart counter.
                }

                // mozoghat, fade out
                player.States.InMoveable = false;
                player.States.LocalTeleport = false;
            });

            return task;
        }

        void ITeleportStrategy.DoTeleport(Player player)
        {
            DoTeleportAsync(player);
        }
    }
}