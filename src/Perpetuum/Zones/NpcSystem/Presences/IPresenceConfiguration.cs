
namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface IPresenceConfiguration
    {
        int ID { get; }

        string Name { get; }
        Area Area { get; }
        int? SpawnId { get; }
        string Note { get; }
        bool Roaming { get; }
        int RoamingRespawnSeconds { get; }
        PresenceType PresenceType { get; }
        int MaxRandomFlock { get; }

        int? RandomCenterX { get; }
        int? RandomCenterY { get; }
        int? RandomRadius { get; }

        int? DynamicLifeTime { get; }
        bool IsRespawnAllowed { get; }

        int? InterzoneGroupId { get; }
        int? GrowthSeconds { get; }

        int ZoneID { get; }

        Position RandomCenter { get; }
    }
}
