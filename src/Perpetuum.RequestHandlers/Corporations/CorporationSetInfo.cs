using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationSetInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);
                var corporation = character.GetCorporation();

                //deal with taxrate
                if (request.Data.TryGetValue(k.taxRate, out int taxRate))
                {
                    corporation.CanSetTaxRate(character).ThrowIfFalse(ErrorCodes.AccessDenied);
                    corporation.TaxRate = taxRate;
                }


                //deal with private profile
                if (request.Data.TryGetValue(k.privateProfile, out Dictionary<string, object> privateProfile))
                {
                    corporation.IsAnyRole(character, CorporationRole.PRManager, CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.AccessDenied);
                    corporation.SetPrivateProfile(privateProfile);
                }

                //public profile
                if (request.Data.TryGetValue(k.publicProfile, out Dictionary<string, object> publicProfile))
                {
                    corporation.IsAnyRole(character, CorporationRole.PRManager, CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.AccessDenied);
                    corporation.SetPublicProfile(publicProfile);
                }

                CorporationData.RemoveFromCache(corporation.Eid);

                var corpInfo = corporation.GetInfoDictionaryForMember(character);
                Message.Builder.SetCommand(Commands.CorporationGetMyInfo).WithData(corpInfo).ToCharacters(corporation.GetCharacterMembers()).Send();
                
                scope.Complete();
            }
        }
    }
}