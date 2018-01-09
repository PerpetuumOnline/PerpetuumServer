using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Applications;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDeleteApplication : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var flush = request.Data.GetOrDefault<int>(k.all) == 1;
                var applierCharacter = Character.Get(request.Data.GetOrDefault<int>(k.characterID));

                var corporation = character.GetPrivateCorporationOrThrow();
                corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                if (flush)
                {
                    corporation.GetApplications().DeleteAll();
                }
                else
                {
                    corporation.GetApplicationsByCharacter(applierCharacter).DeleteAll();
                }

                var result = corporation.GetApplications().ToDictionary();
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}