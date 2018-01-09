using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = CorporationDocumentHelper.GetMyDocumentsToDictionary(character);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}