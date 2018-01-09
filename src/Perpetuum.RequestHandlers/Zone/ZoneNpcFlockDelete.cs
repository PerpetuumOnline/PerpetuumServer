using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneNpcFlockDelete : IRequestHandler<IZoneRequest>
    {
        private readonly IFlockConfigurationRepository _repository;

        public ZoneNpcFlockDelete(IFlockConfigurationRepository repository)
        {
            _repository = repository;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var presenceID = request.Data.GetOrDefault<int>(k.presenceID);
                var flockID = request.Data.GetOrDefault<int>(k.flockID);

                var presence = request.Zone.PresenceManager.GetPresences().GetPresenceOrThrow(presenceID);
                var flock = presence.Flocks.GetFlockOrThrow(flockID);
                presence.RemoveFlock(flock);
                _repository.Delete(flock.Configuration);

                //full list as result
                var result = request.Zone.PresenceManager.GetPresences().ToDictionary("p", p => p.ToDictionary(true));
                Message.Builder.SetCommand(Commands.ZoneListPresences).WithData(result).ToCharacter(character).Send();
                
                scope.Complete();
            }
        }
    }
}