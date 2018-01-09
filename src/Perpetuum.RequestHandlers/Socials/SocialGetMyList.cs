using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Socials
{
    public class SocialGetMyList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var social = request.Session.Character.GetSocial();
            Message.Builder.FromRequest(request).SetData(k.friends, social.ToDictionary()).Send();
        }
    }
}