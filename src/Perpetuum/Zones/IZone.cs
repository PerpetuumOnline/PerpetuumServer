using System.Collections.Generic;
using System.Drawing;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Relics;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Decors;
using Perpetuum.Zones.Environments;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.SafeSpawnPoints;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Terraforming;
using Perpetuum.Zones.ZoneEntityRepositories;

namespace Perpetuum.Zones
{
    public interface IZone 
    {
        [NotNull] ZoneConfiguration Configuration { get; }

        int Id { get; }
        Size Size { get; }

        IEnumerable<Unit> Units { get; }
        IEnumerable<Player> Players { get; }

        [CanBeNull]
        Unit GetUnit(long eid);

        [CanBeNull]
        Player GetPlayer(long eid);

        ITerrain Terrain { get; }
        CorporationHandler CorporationHandler { get; }
        IPlantHandler PlantHandler { get; }
        IBeamService Beams { get; }
        IWeatherService Weather { get; }
        IDecorHandler DecorHandler { get; }
        IEnvironmentHandler Environment { get; }
        IPresenceManager PresenceManager { get; }
        ISafeSpawnPointsRepository SafeSpawnPoints { get; }
        PBSHighwayHandler HighwayHandler { get; }
        TerraformHandler TerraformHandler { get; }
        MiningLogHandler MiningLogHandler { get; }
        RelicManager RelicManager { get; }

        IZoneUnitService UnitService { get; }
        IZoneEnterQueueService EnterQueueService { get; }

        ILogger<ChatLogEvent> ChatLogger { get; }

        void AddUnit(Unit unit);
        void RemoveUnit(Unit unit);
        void SetGang(Player player);

        void Enter(Character character,Command replyCommand);
    }
}