using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.TechTree;

namespace Perpetuum.RequestHandlers.TechTree
{
    public class TechTreeInfo : TechTreeRequestHandler
    {
        private readonly ITechTreeInfoService _infoService;

        public TechTreeInfo(ITechTreeInfoService infoService)
        {
            _infoService = infoService;
        }

        public override void HandleRequest(IRequest request)
        {
            var info = new Dictionary<string, object>
            {
                {"groups",_infoService.GetGroupInfos().Values.ToDictionary("g",g => g.ToDictionary())},
                {"nodes",_infoService.GetNodes().Values.ToDictionary("n",n => n.ToDictionary())},
                {"corporationPriceMultiplier",_infoService.CorporationPriceMultiplier}
            };

            Message.Builder.FromRequest(request).WithData(info).Send();
        }
    }
}