using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelJoin : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelJoin(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var channelName = request.Data.GetOrDefault<string>(k.channel);
                var password = request.Data.GetOrDefault<string>(k.password);

                var character = request.Session.Character;
                _channelManager.JoinChannel(channelName, character, ChannelMemberRole.Undefined, password);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}