using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentOpen : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var documentIds = request.Data.GetOrDefault<int[]>(k.ID);

            documentIds.Length.ThrowIfLessOrEqual(0,ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var documents = new List<CorporationDocument>();

            foreach (var documentId in documentIds)
            {
                if (CorporationDocumentHelper.CheckRegisteredAccess(documentId, character, out var corporationDocument) != ErrorCodes.NoError)
                    continue;

                corporationDocument.ReadBody();
                documents.Add(corporationDocument);
            }

            var result = CorporationDocumentHelper.GenerateResultFromDocuments(documents);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}