using Perpetuum.Players;

namespace Perpetuum.Zones.Teleporting.Strategies
{
    public interface ITeleportStrategy
    {
        void DoTeleport(Player player);
    }

    public interface ITeleportStrategyFactories
    {
        TeleportWithinZone.Factory TeleportWithinZoneFactory { get; }
        TeleportToAnotherZone.Factory TeleportToAnotherZoneFactory { get; }
        TrainingExitStrategy.Factory TrainingExitStrategyFactory { get; }
    }

}