using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ItemCountOnZone : IRequestHandler
    {
        private readonly IEntityServices _entityServices;
        private readonly IZoneManager _zoneManager;

        public ItemCountOnZone(IEntityServices entityServices,IZoneManager zoneManager)
        {
            _entityServices = entityServices;
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var zoneId = request.Data.GetOrDefault<int>(k.zoneID);
            var all = request.Data.GetOrDefault<int>(k.all) == 1;

            if (_zoneManager.GetZone(zoneId) is TrainingZone)
            {
                all = true;
            }

            IList<IDataRecord> records;

            var publicContainer = _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_CONTAINER);

            var character = request.Session.Character;
            if (all)
            {
                records = Db.Query().CommandText("itemCountOnBases")
                    .SetParameter("@owner", character.Eid)
                    .SetParameter("@publicContainerDefinition", publicContainer.Definition)
                    .Execute();
            }
            else
            {
                records = Db.Query().CommandText("itemCountOnZone")
                    .SetParameter("@owner", character.Eid)
                    .SetParameter("@publicContainerDefinition", publicContainer.Definition)
                    .SetParameter("@zoneId", zoneId)
                    .Execute();
            }


            var result = new Dictionary<string, object>(records.Count + 1);

            var perTerminal =
                records.ToDictionary("c", r =>
                {
                    return new Dictionary<string, object>(3)
                    {
                        {"ftEid", r.GetValue<long>("fteid")},
                        {k.containerEID, r.GetValue<long>("containereid")},
                        {k.amount, r.GetValue<int>("amount")}
                    };
                });

            result.Add(k.data, perTerminal);

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}