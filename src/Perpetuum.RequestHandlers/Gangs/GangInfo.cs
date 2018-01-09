using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangInfo : IRequestHandler
    {
        private readonly IGangManager _gangManager;

        public GangInfo(IGangManager gangManager)
        {
            _gangManager = gangManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var messageBuilder = Message.Builder.FromRequest(request);

            var gang = _gangManager.GetGangByMember(character);
            if (gang == null)
                messageBuilder.WithEmpty();
            else
                messageBuilder.WithData(gang.ToDictionary());

            messageBuilder.Send();
        }
    }
}