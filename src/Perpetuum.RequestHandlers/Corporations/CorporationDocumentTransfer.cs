using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentTransfer : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var documentId = request.Data.GetOrDefault<int>(k.ID);
                var targetCharacter = Character.Get(request.Data.GetOrDefault<int>(k.target));

                character.GetPrivateCorporationOrThrow();
                character.IsFriend(targetCharacter).ThrowIfFalse(ErrorCodes.CharacterIsNotAFriend);

                CorporationDocument corporationDocument;
                CorporationDocumentHelper.CheckOwnerAccess(documentId, character, out corporationDocument).ThrowIfError();
            
                //check amount and stuff
                CorporationDocumentHelper.OnDocumentTransfer(corporationDocument, targetCharacter).ThrowIfError();

                corporationDocument._ownerCharacter = targetCharacter;
                corporationDocument.UpdateOwnerToSql().ThrowIfError();

                //unregister document and all viewer character
                CorporationDocumentHelper.DeleteViewerByDocumentId(documentId);

                var targetResult =  CorporationDocumentHelper.GetMyDocumentsToDictionary(targetCharacter);
                var senderResult = CorporationDocumentHelper.GetMyDocumentsToDictionary( character);

                Message.Builder.FromRequest(request).WithData(senderResult).Send();
                Message.Builder.SetCommand(request.Command).WithData(targetResult).ToCharacter(targetCharacter).Send();
                
                scope.Complete();
            }
        }
    }
}