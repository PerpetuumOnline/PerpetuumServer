using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.RequestHandlers.Production
{
    public class GetResearchLevels : IRequestHandler
    {
        private readonly Dictionary<string, object> _researchLevelInfos;

        public GetResearchLevels(IProductionDataAccess productionDataAccess)
        {
            _researchLevelInfos = productionDataAccess.ResearchLevels.Values.ToDictionary("r", r => r.ToDictionary());
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_researchLevelInfos).Send();
        }
    }
}