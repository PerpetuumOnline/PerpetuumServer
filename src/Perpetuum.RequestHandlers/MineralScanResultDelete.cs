using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.RequestHandlers
{
    public class MineralScanResultDelete : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var resultIds = request.Data.GetOrDefault<int[]>(k.items);

                if (resultIds.Length > 1000)
                {
                    // ezt azert,h ne szalljon el az sql, kell kliensbe is egy ilyen limit
                    Array.Resize(ref resultIds, 1000);
                }

                var character = request.Session.Character;
                var repo = new MineralScanResultRepository(character);

                foreach (var id in resultIds)
                {
                    repo.DeleteById(id);
                }

                Transaction.Current.OnCommited(() =>
                {
                    var info = new Dictionary<string, object> { { k.items, resultIds } };
                    Message.Builder.FromRequest(request)
                        .WithData(info)
                        .Send();
                });
                
                scope.Complete();
            }
        }
    }
}
