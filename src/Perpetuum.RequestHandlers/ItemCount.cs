using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ItemCount : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public ItemCount(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var publicContainer = _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_CONTAINER);
            var lista = Db.Query().CommandText("itemCount")
                .SetParameter("@owner", character.Eid)
                .SetParameter("@publicContainerDefinition", publicContainer.Definition)
                .Execute()
                .ToDictionary("c", r => new Dictionary<string, object>
                {
                    {k.eid, DataRecordExtensions.GetValue<long>(r, 0)},
                    {k.amount, DataRecordExtensions.GetValue<int>(r, 1)}
                });

            var result = new Dictionary<string, object> { { k.data, lista } };
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}