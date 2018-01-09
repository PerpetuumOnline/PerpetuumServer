using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDocumentCreate : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var documentType = (CorporationDocumentType)request.Data.GetOrDefault<int>(k.type);
                var body = request.Data.GetOrDefault<string>(k.body);
                var registered = request.Data.GetOrDefault<int[]>(k.members);
                var writeAccess = request.Data.GetOrDefault<int[]>(k.writeAccess);
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;

                int[] finalRegistered = null;
                int[] finalWriteAccess = null;
                if (registered != null)
                {
                    finalRegistered = registered.Where(d => d != character.Id).Distinct().ToArray();

                    if (writeAccess != null)
                    {
                        finalWriteAccess = finalRegistered.Intersect(writeAccess).Where(d => d != character.Id).ToArray();
                    }

                }

                documentType.ThrowIfEqual(CorporationDocumentType.terraformProject,ErrorCodes.InvalidDocumentType);

                CorporationDocumentHelper.GetDocumentConfig(documentType, out var documentConfig).ThrowIfError();

                documentConfig.OnCreate(character, useCorporationWallet).ThrowIfError();

                DateTime? validUntil = null;
                if (documentConfig.IsRentable)
                {
                    validUntil = DateTime.Now.AddDays(documentConfig.rentPeriodDays);
                }

                CorporationDocument.CreateNewToSql( character, documentType, validUntil, body, out var corporationDocument).ThrowIfError();

                corporationDocument.SetRegistration(finalRegistered, finalWriteAccess);
            
                var result = new Dictionary<string, object>
                {
                    {k.document, corporationDocument.ToDictionary()}
                };

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}