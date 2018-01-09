using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentUpdateBody : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var documentId = request.Data.GetOrDefault<int>(k.ID);
                var bodyStr = request.Data.GetOrDefault<string>(k.body);
                var version = request.Data.GetOrDefault<int>(k.version);

            
                CorporationDocument corporationDocument;
                CorporationDocumentHelper.CheckRegisteredAccess(documentId, character, out corporationDocument, true).ThrowIfError();

                version.ThrowIfLess(corporationDocument.version,ErrorCodes.DocumentVersionOld);

                corporationDocument.SetBody(bodyStr);
                corporationDocument.WriteBody().ThrowIfError();

                var result = CorporationDocumentHelper.GenerateResultFromDocuments(new[] {corporationDocument});
            
                //itt terjesztunk
                var registered = new List<Character> {corporationDocument._ownerCharacter};

                registered.AddRange(CorporationDocumentHelper.GetRegisteredCharactersFromDocument(documentId));

                Message.Builder.SetCommand(Commands.CorporationDocumentUpdateBody).WithData(result).ToCharacters(registered).Send();
                
                scope.Complete();
            }
        }
    }
}