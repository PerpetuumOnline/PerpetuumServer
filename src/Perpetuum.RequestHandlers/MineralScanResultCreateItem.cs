using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.RequestHandlers
{
    public class MineralScanResultCreateItem : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var repo = new MineralScanResultRepository(character);

                var scanResultId = request.Data.GetOrDefault<int>(k.ID);
                var scanResult = repo.Get(scanResultId).ThrowIfNull(ErrorCodes.MineralScanResultNotFound);
                var item = scanResult.ToItem();
                item.Owner = character.Eid;

                var container = character.GetPublicContainerWithItems();
                container.AddItem(item, false);
                container.Save();

                Transaction.Current.OnCommited(() => Message.Builder.FromRequest(request).WithData(container.ToDictionary()).Send());
                
                scope.Complete();
            }
        }
    }
}
