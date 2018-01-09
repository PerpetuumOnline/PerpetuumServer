using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketAvailableItems : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public MarketAvailableItems(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            var queryStr = "select distinct itemdefinition from marketitems where marketeid=@marketEID and (( formembersof is not null and @fmo=formembersof) or ( formembersof is null ))";

            var corporders = request.Data.GetOrDefault<int>(k.corporationOnly) == 1;
            if (corporders)
            {
                queryStr = "select distinct itemdefinition from marketitems where marketeid=@marketEID and formembersof is not null and @fmo=formembersof";
            }

            var marketEid = request.Data.GetOrDefault<long>(k.eid);

            var character = request.Session.Character;
            var definitionList = Db.Query().CommandText(queryStr)
                .SetParameter("@marketEID", marketEid)
                .SetParameter("@fmo", character.CorporationEid)
                .Execute()
                .Select(e => DataRecordExtensions.GetValue<int>(e, 0)).ToArray();

            //just in case
            if (definitionList.Length == 0)
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
                return;
            }

            var categoryFlags = new List<long>();

            foreach (var definition in definitionList)
            {
                if (!_entityServices.Defaults.TryGet(definition, out EntityDefault ed))
                {
                    Logger.Error("disabled definition on market: " + definition);
                    continue;
                }

                categoryFlags.Add((long)ed.CategoryFlags);
            }

            //var categoryFlags = (from d in definitionList select (long) EntityDefaultHelper.entityDefaults[d].categoryFlags).Distinct().ToArray();

            var result = new Dictionary<string, object>
            {
                {k.definition, definitionList},
                {k.categoryFlags, categoryFlags.Distinct().ToArray()},
                {k.corporationOnly, corporders},
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}