using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneNpcFlockSet : IRequestHandler<IZoneRequest>
    {
        private readonly IEntityDefaultReader _defaultReader;
        private readonly IFlockConfigurationRepository _repository;
        private readonly FlockConfigurationBuilder.Factory _flockConfigurationBuilderFactory;

        public ZoneNpcFlockSet(IEntityDefaultReader defaultReader,IFlockConfigurationRepository repository,FlockConfigurationBuilder.Factory flockConfigurationBuilderFactory)
        {
            _defaultReader = defaultReader;
            _repository = repository;
            _flockConfigurationBuilderFactory = flockConfigurationBuilderFactory;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var ID = request.Data.GetOrDefault<int>(k.ID);
                var presenceID = request.Data.GetOrDefault<int>(k.presenceID);
                var definition = request.Data.GetOrDefault<int>(k.definition);
                var spawnOriginX = request.Data.GetOrDefault<int>(k.spawnOriginX);
                var spawnOriginY = request.Data.GetOrDefault<int>(k.spawnOriginY);
                var respawnMultiplierLow = request.Data.GetOrDefault(k.respawnMultiplierLow, 0.75);

                //instafix dict
                request.Data[k.respawnMultiplierLow] = respawnMultiplierLow;

                var ed = _defaultReader.Get(definition);
                if (!ed.CategoryFlags.IsCategory(CategoryFlags.cf_npc))
                    throw new PerpetuumException(ErrorCodes.DefinitionNotSupported);

                var presence = request.Zone.PresenceManager.GetPresences().GetPresenceOrThrow(presenceID);

                var origFlock = presence.Flocks.GetFlockOrThrow(ID);
                presence.RemoveFlock(origFlock);

                var inputDict = new Dictionary<string, object>(request.Data) { { k.spawnOrigin, new Position(spawnOriginX, spawnOriginY) } };

                var builder = _flockConfigurationBuilderFactory();
                builder.FromDictionary(inputDict);
                builder.SetID(ID);
                var configuration = builder.Build();
                _repository.Update(configuration);

                presence.CreateAndAddFlock(configuration);

                var result = request.Zone.PresenceManager.GetPresences().ToDictionary("p", p => p.ToDictionary(true));
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}