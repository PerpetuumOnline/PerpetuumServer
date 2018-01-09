using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.TechTree;

namespace Perpetuum.RequestHandlers.TechTree
{
    public abstract class TechTreeRequestHandler : IRequestHandler
    {
        public abstract void HandleRequest(IRequest request);

        protected static void SendInfoToCorporation(ITechTreeService techTreeService,long corporationEid)
        {
            var info = new Dictionary<string, object>();
            techTreeService.AddInfoToDictionary(corporationEid, info);
            var points = new TechTreePointsHandler(corporationEid);
            points.AddAvailablePointsToDictionary(info);

            Message.Builder.SetCommand(Commands.TechTreeCorporationInfo)
                .WithData(info)
                .ToCorporation(corporationEid, PresetCorporationRoles.CAN_LIST_TECHTREE)
                .Send();
        }
    }
}