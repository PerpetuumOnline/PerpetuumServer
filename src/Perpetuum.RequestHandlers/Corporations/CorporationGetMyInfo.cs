using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationGetMyInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporation = character.GetCorporation();
            //get the full info for a member
            var corpInfo = corporation.GetInfoDictionaryForMember(character);
            Message.Builder.FromRequest(request).WithData(corpInfo).Send();
        }
    }
}