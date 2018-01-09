using System.Collections.Generic;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class RequestInfiniteBox : IRequestHandler
    {
        private const double INFINITE_BOX_CONTAINER_PRICE = 5000;

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
                character.SubtractFromWallet(TransactionType.BoxRequest, INFINITE_BOX_CONTAINER_PRICE);
                character.GetCurrentDockingBase().AddCentralBank(TransactionType.BoxRequest, INFINITE_BOX_CONTAINER_PRICE);

                var box = InfiniteBoxContainer.Create();
                box.Owner = character.Eid;

                var publicContainer = character.GetPublicContainerWithItems();
                publicContainer.AddItem(box, false);
                publicContainer.Save();

                var informData = publicContainer.ToDictionary();
                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.container, informData } }).Send();
                
                scope.Complete();
            }
        }
    }
}