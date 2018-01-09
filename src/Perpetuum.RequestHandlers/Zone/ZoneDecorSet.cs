using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Decors;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDecorSet : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var decorDescription = new DecorDescription
                {
                    id = request.Data.GetOrDefault<int>(k.ID),
                    definition = request.Data.GetOrDefault<int>(k.definition),
                    quaternionX = request.Data.GetOrDefault<double>(k.quaternionX),
                    //shifted
                    quaternionY = request.Data.GetOrDefault<double>(k.quaternionY),
                    quaternionZ = request.Data.GetOrDefault<double>(k.quaternionZ),
                    quaternionW = request.Data.GetOrDefault<double>(k.quaternionW),
                    scale = request.Data.GetOrDefault<double>(k.scale),
                    position = new Position(request.Data.GetOrDefault<int>(k.x), request.Data.GetOrDefault<int>(k.y), request.Data.GetOrDefault<int>(k.z)),
                    zoneId = request.Zone.Id,
                    fadeDistance = request.Data.GetOrDefault<double>(k.fadeDistance),
                    category = request.Data.GetOrDefault<int>(k.category),
                    changed = true
                };

                //check definition
                EntityDefault.Get(decorDescription.definition).CategoryFlags.IsCategory(CategoryFlags.cf_decor).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);

                DecorDescription oldDescription;
                request.Zone.DecorHandler.Decors.TryGetValue(decorDescription.id, out oldDescription).ThrowIfFalse(ErrorCodes.ItemNotFound);

                if (oldDescription.locked)
                {
                    request.Zone.DecorHandler.SpreadDecorChanges(oldDescription);
                    throw new PerpetuumException(ErrorCodes.DecorLocked);
                }

                //save to sql
                request.Zone.DecorHandler.UpdateDecorSql(decorDescription).ThrowIfError();

                Transaction.Current.OnCommited(() =>
                {
                    //save to ram
                    request.Zone.DecorHandler.SetDecor(decorDescription);
                    request.Zone.DecorHandler.SpreadDecorChanges(decorDescription);
                });
                
                scope.Complete();
            }
        }
    }
}