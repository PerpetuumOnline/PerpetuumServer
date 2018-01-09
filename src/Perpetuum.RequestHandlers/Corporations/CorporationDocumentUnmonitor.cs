using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentUnmonitor : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var documentId = request.Data.GetOrDefault<int>(k.ID);
 
            CorporationDocumentHelper.UnRegisterCharacterFromDocument(documentId,character);

#if DEBUG
            Message.Builder.FromRequest(request).WithOk().Send();
#endif
        }
    }
}