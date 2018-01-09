using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Decors;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDecorAdd : IRequestHandler<IZoneRequest>
    {
        private readonly IEntityServices _entityServices;

        public ZoneDecorAdd(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var zone = request.Zone;
                var decorDescription = new DecorDescription
                {
                    definition = request.Data.GetOrDefault<int>(k.definition),
                    quaternionX = request.Data.GetOrDefault<double>(k.quaternionX),
                    //shifted
                    quaternionY = request.Data.GetOrDefault<double>(k.quaternionY),
                    quaternionZ = request.Data.GetOrDefault<double>(k.quaternionZ),
                    quaternionW = request.Data.GetOrDefault<double>(k.quaternionW),
                    scale = request.Data.GetOrDefault<double>(k.scale),
                    position = new Position(request.Data.GetOrDefault<int>(k.x), request.Data.GetOrDefault<int>(k.y), request.Data.GetOrDefault<int>(k.z)),
                    zoneId = zone.Id,
                    category = request.Data.GetOrDefault<int>(k.category),
                    changed = true,
                    fadeDistance = request.Data.GetOrDefault<double>(k.fadeDistance)
                };

                //check definition
                _entityServices.Defaults.Get(decorDescription.definition).CategoryFlags.IsCategory(CategoryFlags.cf_decor).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);

                //insert to sql
                int newId;
                zone.DecorHandler.InsertDecorSql(decorDescription, out newId).ThrowIfError();

                Transaction.Current.OnCommited(() =>
                {
                    //set to ram
                    decorDescription.id = newId;
                    zone.DecorHandler.SetDecor(decorDescription);
                    zone.DecorHandler.SpreadDecorChanges(decorDescription);
                });
                
                scope.Complete();
            }
        }
    }
}