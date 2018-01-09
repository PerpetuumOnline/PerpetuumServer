using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers
{
    public class ReturnCorporationOwnderItems : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public ReturnCorporationOwnderItems(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var corporationEiDs = Db.Query().CommandText("select eid from corporations where defaultcorp=0 and active=1").Execute()
                    .Select(r => r.GetValue<long>(0))
                    .ToList();

                var publicContainer = _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_CONTAINER);
                var corporateHangar = _entityServices.Defaults.GetByName(DefinitionNames.CORPORATE_HANGAR_STANDARD);

                var publicContainerEIds = Db.Query().CommandText("select eid from entities where definition=@publicContainerDef")
                    .SetParameter("@publicContainerDef", publicContainer.Definition)
                    .Execute()
                    .Select(r => r.GetValue<long>(0))
                    .ToList();

                foreach (var publicContainerEId in publicContainerEIds)
                {
                    Logger.Info("processing public container: eid:" + publicContainerEId);

                    var pairsInPublicContainer = Db.Query().CommandText("getTreeNonFiltered")
                        .SetParameter("@rootEID", publicContainerEId)
                        .Execute()
                        .Select(r => new KeyValuePair<long, long?>(r.GetValue<long>(0), r.GetValue<long>(1)))
                        .ToList();

                    Logger.Info(pairsInPublicContainer.Count + " items where found in public container");


                    foreach (var pair in pairsInPublicContainer)
                    {
                        if (pair.Value == null)
                        {
                            Logger.Info("an item with owner=NULL was found. EID:" + pair.Key);
                            continue;
                        }

                        if (!corporationEiDs.Contains((long)pair.Value))
                            continue;

                        Logger.Info("item belongs to corp. itemEID:" + pair.Key + "  corpEID:" + pair.Value);

                        //a corporation owned item was found

                        var corpStorages = Db.Query().CommandText("select eid from entities where definition=@corpHangar and owner=@owner")
                            .SetParameter("@owner", pair.Value)
                            .SetParameter("@corpHangar", corporateHangar.Definition)
                            .Execute()
                            .Select(r => r.GetValue<long>(0)).ToList();

                        if (corpStorages.Count == 0)
                        {
                            Logger.Info("no corp storage was found for corporation:" + pair.Value);
                            continue;
                        }

                        var storageEid = corpStorages.First();
                        Logger.Info(corpStorages.Count + " corp storages were found. choosen one: " + storageEid);

                        var res = Db.Query().CommandText("update entities set parent=@corpStorage where eid=@itemEID")
                            .SetParameter("@corpStorage", storageEid)
                            .SetParameter("@itemEID", pair.Key)
                            .ExecuteNonQuery();

                        if (res != 1)
                        {
                            Logger.Error("sql update error on eid:" + pair.Key);
                            continue;
                        }

                        Logger.Info("item moved to corp storage successfully. " + pair.Key);
                    }
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}