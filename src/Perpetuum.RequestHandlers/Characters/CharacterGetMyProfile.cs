using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterGetMyProfile : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var profile = character.GetFullProfile();
            Message.Builder.FromRequest(request).WithData(profile).Send();
        }
    }
}