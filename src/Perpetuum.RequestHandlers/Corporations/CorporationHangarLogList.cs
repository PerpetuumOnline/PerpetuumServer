using Perpetuum.Containers;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarLogList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var hangarEID = request.Data.GetOrDefault<long>(k.eid);
            var offsetInDays = request.Data.GetOrDefault<int>(k.offset);

            //load the hangar with different checks
            var corporateHangar = character.GetCorporation().GetHangar(hangarEID, character, ContainerAccess.LogList);

            Message.Builder.FromRequest(request)
                .WithData(corporateHangar.ContainerLogger.LogsToDictionary(offsetInDays))
                .Send();
        }
    }
}