using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentRegisterSet : IRequestHandler
     {
        public void HandleRequest(IRequest request)
         {
             using (var scope = Db.CreateTransaction())
             {
                 var character = request.Session.Character;
                 var documentId = request.Data.GetOrDefault<int>(k.ID);
                 var registeredList = request.Data.GetOrDefault<int[]>(k.members);
                 var writeMembers = request.Data.GetOrDefault<int[]>(k.writeAccess);

                 var finalRegisteredList = registeredList.Distinct().Where(d => d != character.Id).ToArray();
                 var finalWriteMembers = finalRegisteredList.Intersect(writeMembers).Distinct().ToArray();

                 CorporationDocumentHelper.CheckOwnerAccess(documentId, character, out var corporationDocument).ThrowIfError();

                 finalRegisteredList.Length.ThrowIfGreater(CorporationDocument.MAX_REGISTERED_MEMBERS, ErrorCodes.MaximumAllowedRegistrationExceeded);

                 corporationDocument.SetRegistration(finalRegisteredList, finalWriteMembers);
             
                 var result = new Dictionary<string, object>
                 {
                     {k.ID, documentId},
                     {k.registered, finalRegisteredList},
                     {k.writeAccess, finalWriteMembers}
                 };

                 Message.Builder.FromRequest(request).WithData(result).Send();
                 
                 scope.Complete();
             }
         }
     }

}
