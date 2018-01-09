using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.RequestHandlers
{
    public class MineralScanResultMove : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var resultIds = request.Data.GetOrDefault<int[]>(k.items);
                var folder = request.Data.GetOrDefault<string>(k.folder);

                if (resultIds.Length > 1000)
                {
                    // ezt azert,h ne szalljon el az sql, kell kliensbe is egy ilyen limit
                    Array.Resize(ref resultIds, 1000);
                }

                var character = request.Session.Character;
                var repo = new MineralScanResultRepository(character);
                foreach (var id in resultIds)
                {
                    repo.MoveToFolderById(id,folder);
                }

                Transaction.Current.OnCommited(() =>
                {
                    var info = new Dictionary<string, object> { { k.items, resultIds }, { k.folder, folder } };
                    Message.Builder.FromRequest(request).WithData(info).Send();
                });
                
                scope.Complete();
            }
        }
    }

}
