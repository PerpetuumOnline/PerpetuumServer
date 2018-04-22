using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Zones.Finders.PositionFinders;

namespace Perpetuum.Zones.Teleporting.Strategies
{
    public class TeleportToAnotherZone : ITeleportStrategy
    {
        private readonly IZone _targetZone;

        public delegate TeleportToAnotherZone Factory(IZone targetZone);

        public TeleportToAnotherZone(IZone targetZone)
        {
            _targetZone = targetZone;
        }

        public Position TargetPosition { get; set; }

        public void DoTeleport(Player player)
        {
            if (!player.InZone )
                return;

            player.States.Teleport = true;
            Entity.Repository.ForceUpdate(player);

            var character = player.Character;
            character.ZoneId = _targetZone.Id;
            character.ZonePosition = TargetPosition;

            Transaction.Current.OnCommited(() =>
            {
                player.RemoveFromZone();
                Task.Delay(1000).ContinueWith(t => _targetZone.Enter(character,Commands.TeleportUse));
            });
        }

        [CanBeNull]
        public Task DoTeleportAsync(Player player)
        {
            // get a valid position to teleport to.
            var position = new ClosestWalkablePositionFinder(_targetZone, TargetPosition);
            if (!position.Find(out Position validPosition))
            {
                return null;
            }

            player.States.Teleport = true;
            Entity.Repository.ForceUpdate(player);
            var character = player.Character;
            character.ZoneId = _targetZone.Id;
            character.ZonePosition = validPosition;

            var task = Task.Delay(10).ContinueWith(t =>
            {
                player.RemoveFromZone();
                player.CurrentSpeed = 0.0;
                _targetZone.Enter(character, Commands.TeleportUse);
                player.States.InMoveable = false;
                player.States.LocalTeleport = false;
            });

            return task;
        }
    }
}