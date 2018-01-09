using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionUnrepairItem : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var target = request.Data.GetOrDefault<long>(k.target);
                var ratio = request.Data.GetOrDefault<double>(k.ratio);

                var entity = Item.GetOrThrow(target);
                entity.Health = entity.Health * ratio;
                entity.Save();

                var replyDict = new Dictionary<string, object>(2)
                {
                    {k.eid, entity.Eid},
                    {k.health, entity.Health}
                };

                Message.Builder.FromRequest(request).WithData(replyDict).Send();
                
                scope.Complete();
            }
        }
    }
}