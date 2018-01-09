using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelRemoveBan : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelRemoveBan(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var channelName = request.Data.GetOrDefault<string>(k.channel);
                var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));

                var character = request.Session.Character;
                _channelManager.UnBan(channelName, character, member);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}