using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentMonitor : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var documentId = request.Data.GetOrDefault<int>(k.ID);

            CorporationDocument corporationDocument;
            CorporationDocumentHelper.CheckRegisteredAccess(documentId,character, out corporationDocument).ThrowIfError();
            CorporationDocumentHelper.RegisterCharacterToDocument(documentId, character);
#if DEBUG
            Message.Builder.FromRequest(request).WithOk().Send();
#endif
        }
    }
}