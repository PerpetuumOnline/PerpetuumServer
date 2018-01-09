using System.Linq;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentDelete : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var documentId = request.Data.GetOrDefault<int>(k.ID);


                CorporationDocumentHelper.CheckOwnerAccess(documentId, character, out var corporationDocument).ThrowIfError();

                corporationDocument.Delete().ThrowIfError();

                corporationDocument.DeleteAllRegistered();
            
                var registered = CorporationDocumentHelper.GetRegisteredCharactersFromDocument(documentId).ToList();
            
                //beleaddoljuk azt is aki letorolte, meg mindenkit aki epp nezi
                if (!registered.Contains(character))
                {
                    registered.Add(character);
                }

                CorporationDocumentHelper.DeleteViewerByDocumentId(documentId);

                var result = CorporationDocumentHelper.GetMyDocumentsToDictionary( character);
                Message.Builder.SetCommand(request.Command).WithData(result).ToCharacters(registered).Send();
                
                scope.Complete();
            }
        }
    }
}