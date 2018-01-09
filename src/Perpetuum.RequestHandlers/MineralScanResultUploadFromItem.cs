using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.RequestHandlers
{
    public class MineralScanResultUploadFromItem : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var itemEid = request.Data.GetOrDefault<long>(k.itemEID);

                var container = Container.GetWithItems(containerEid, character, ContainerAccess.Remove);
                container.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.AccessDenied);

                var item = container.GetItemOrThrow(itemEid).ThrowIfNotType<MineralScanResultItem>(ErrorCodes.DefinitionNotSupported);

                var scanResult = item.ToScanResult().ThrowIfNull(ErrorCodes.ServerError);

                var repo = new MineralScanResultRepository(character);
                repo.InsertOrThrow(scanResult);

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.container, container.ToDictionary()}, 
                        {k.scanResult, scanResult.ToDictionary()}
                    };

                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}
