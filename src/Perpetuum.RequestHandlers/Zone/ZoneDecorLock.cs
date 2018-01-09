using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Decors;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDecorLock : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var zone = request.Zone;

                var id = request.Data.GetOrDefault<int>(k.ID);
                var lockValue = request.Data.GetOrDefault<int>(k.locked) == 1;

                DecorDescription oldDescription;
                zone.DecorHandler.Decors.TryGetValue(id, out oldDescription).ThrowIfFalse(ErrorCodes.ItemNotFound);

                oldDescription.locked = lockValue;

                //save to sql
                zone.DecorHandler.UpdateDecorSql(oldDescription).ThrowIfError();

                Transaction.Current.OnCommited(() =>
                {
                    //save to ram
                    zone.DecorHandler.SetDecor(oldDescription);
                    zone.DecorHandler.SpreadDecorChanges(oldDescription);
                });
                
                scope.Complete();
            }
        }
    }
}