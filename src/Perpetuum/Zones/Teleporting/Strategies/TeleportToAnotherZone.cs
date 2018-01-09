using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Players;

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
    }
}