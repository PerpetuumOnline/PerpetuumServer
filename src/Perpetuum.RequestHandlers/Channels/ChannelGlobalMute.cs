using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelGlobalMute : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
                var state = request.Data.GetOrDefault<int>(k.state).ToBool();
                character.GlobalMuted = state;
                scope.Complete();
            }
        }
    }
}