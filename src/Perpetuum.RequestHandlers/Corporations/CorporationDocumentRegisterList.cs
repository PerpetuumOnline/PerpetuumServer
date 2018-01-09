using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentRegisterList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var documentId = request.Data.GetOrDefault<int>(k.ID);

            CorporationDocumentHelper.CheckOwnerAccess(documentId, character, out var corporationDocument).ThrowIfError();

            var result = new Dictionary<string, object>
            {
                {k.ID, documentId},
                {k.members, corporationDocument.GetRegisteredDictionary()}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}