using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationSetColor : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var color = request.Data.GetOrDefault<int>(k.color);

                var corporation = character.GetPrivateCorporationOrThrow().CheckAccessAndThrowIfFailed(character,CorporationRole.CEO,CorporationRole.DeputyCEO);

                corporation.SetColor(color);

                var corpInfo = corporation.GetInfoDictionaryForMember(character);
                Message.Builder.FromRequest(request).WithData(corpInfo).Send();

                CorporationData.RemoveFromCache(corporation.Eid);
                
                scope.Complete();
            }
        }
    }
}
