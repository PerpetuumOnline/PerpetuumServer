using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentRent : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var documentId = request.Data.GetOrDefault<int>(k.ID);
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;

                CorporationDocument corporationDocument;
                CorporationDocumentHelper.CheckOwnerAccess(documentId, character, out corporationDocument).ThrowIfError();

                corporationDocument.Rent(character, useCorporationWallet).ThrowIfError();

                var result = CorporationDocumentHelper.GenerateResultFromDocuments(new[] {corporationDocument});
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}