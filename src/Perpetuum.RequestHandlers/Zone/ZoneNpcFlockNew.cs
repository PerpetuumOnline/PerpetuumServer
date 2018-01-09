using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneNpcFlockNew : IRequestHandler<IZoneRequest>
    {
        private readonly IFlockConfigurationRepository _repository;
        private readonly FlockConfigurationBuilder.Factory _flockConfigurationBuilderFactory;

        public ZoneNpcFlockNew(IFlockConfigurationRepository repository,FlockConfigurationBuilder.Factory flockConfigurationBuilderFactory)
        {
            _repository = repository;
            _flockConfigurationBuilderFactory = flockConfigurationBuilderFactory;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var spawnOriginX = request.Data.GetOrDefault<int>(k.spawnOriginX);
                var spawnOriginY = request.Data.GetOrDefault<int>(k.spawnOriginY);
                var presenceID = request.Data.GetOrDefault<int>(k.presenceID);
                var respawnMultiplierLow = request.Data.GetOrDefault(k.respawnMultiplierLow, 0.75);
                //instafix dict
                request.Data[k.respawnMultiplierLow] = respawnMultiplierLow;

                var presence = request.Zone.PresenceManager.GetPresences().GetPresenceOrThrow(presenceID);

                var inputDict = new Dictionary<string, object>(request.Data)
                {
                    {k.spawnOrigin, new Position(spawnOriginX, spawnOriginY)}
                };

                var builder = _flockConfigurationBuilderFactory();
                builder.FromDictionary(inputDict);
                var configuration = builder.Build();
                _repository.Insert(configuration);

                var flock = presence.CreateAndAddFlock(configuration);
                flock.SpawnAllMembers();

                //full list as result
                var result = request.Zone.PresenceManager.GetPresences().ToDictionary("p", p => p.ToDictionary(true));
                Message.Builder.SetCommand(Commands.ZoneListPresences).WithData(result).ToCharacter(character).Send();
                
                scope.Complete();
            }
        }
    }
}