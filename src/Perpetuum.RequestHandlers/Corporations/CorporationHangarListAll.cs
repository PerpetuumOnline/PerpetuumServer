using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarListAll : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public CorporationHangarListAll(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporation = character.GetPrivateCorporationOrThrow();

            const string queryStr = @"SELECT e.eid,e.parent FROM dbo.entities e 
 WHERE 
 e.eid in (SELECT eid FROM dbo.getLiveDockingbaseChildren() WHERE definition=@hangarDef)
 AND
(SELECT COUNT(*) FROM dbo.entities WHERE parent=e.eid AND owner=@owner)>0";

            var storage = _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_CORPORATE_HANGARS_STORAGE);

            var storageEidsWithHangars =
                Db.Query().CommandText(queryStr)
                    .SetParameter("@owner", corporation.Eid)
                    .SetParameter("@hangarDef", storage.Definition)
                    .Execute();


            var result = new Dictionary<string, object>();
            var counter = 0;

            foreach (var record in storageEidsWithHangars)
            {
                var storageEid = record.GetValue<long>(0);
                var storageParent = record.GetValue<long>(1);

                foreach (var hangar in _entityServices.Repository.GetFirstLevelChildrenByOwner(storageEid, corporation.Eid))
                {
                    var info = hangar.ToDictionary();
                    info.Remove(k.items);
                    info[k.noItemsSent] = 1;
                    info[k.baseEID] = storageParent;

                    result.Add("c" + counter++, info);
                }
            }

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}