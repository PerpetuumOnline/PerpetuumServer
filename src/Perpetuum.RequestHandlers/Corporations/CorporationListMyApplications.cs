using Perpetuum.Groups.Corporations.Applications;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationListMyApplications : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = character.GetCorporationApplications().ToDictionary();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}