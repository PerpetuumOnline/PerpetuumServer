using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Applications;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDeleteMyApplication : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var flush = request.Data.GetOrDefault<int>(k.all) == 1;

                if (flush)
                {
                    character.GetCorporationApplications().DeleteAll();
                }
                else
                {
                    var corporationEID = request.Data.GetOrDefault<long>(k.corporationEID);
                    var corporation = PrivateCorporation.GetOrThrow(corporationEID);
                    corporation.GetApplicationsByCharacter(character).DeleteAll();
                }

                var result = character.GetCorporationApplications().ToDictionary();
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}