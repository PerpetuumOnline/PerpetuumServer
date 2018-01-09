using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers
{
    public class GiftOpen : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public GiftOpen(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var giftEid = request.Data.GetOrDefault<long>(k.eid);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var publicContainer = character.GetPublicContainerWithItems();
                var giftItem = (Gift)publicContainer.GetItemOrThrow(giftEid,true).Unstack(1);
                var randomItem = giftItem.Open(publicContainer,character);
                _entityServices.Repository.Delete(giftItem);
                publicContainer.Save();

                var gifts = new Dictionary<int, int> { { randomItem.Definition, randomItem.Quantity } }.ToDictionary("g", g =>
                {
                    var oneEntry = new Dictionary<string, object>
                    {
                        {k.definition, g.Key},
                        {k.quantity, g.Value}
                    };
                    return oneEntry;
                });

                var result = new Dictionary<string, object>
                {
                    {k.container, publicContainer.ToDictionary()},
                    {"gift",gifts},
                };

                Transaction.Current.OnCommited(() => Message.Builder.FromRequest(request).WithData(result).Send());
                
                scope.Complete();
            }
        }
    }
}