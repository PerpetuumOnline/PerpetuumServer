using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Decors;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDecorDelete : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var zone = request.Zone;

                var id = request.Data.GetOrDefault<int>(k.ID);

                DecorDescription oldDescription;
                zone.DecorHandler.Decors.TryGetValue(id, out oldDescription).ThrowIfFalse(ErrorCodes.ItemNotFound);

                if (oldDescription.locked)
                {
                    zone.DecorHandler.SpreadDecorChanges(oldDescription);
                    throw new PerpetuumException(ErrorCodes.DecorLocked);
                }

                //save to sql
                zone.DecorHandler.DeleteDecorSql(id).ThrowIfError();

                Transaction.Current.OnCommited(() =>
                {
                    zone.DecorHandler.DeleteDecor(id);
                    zone.DecorHandler.SpreadDecorDelete(id);
                });
                
                scope.Complete();
            }
        }
    }
}