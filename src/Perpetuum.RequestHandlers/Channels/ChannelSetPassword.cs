using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelSetPassword : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelSetPassword(IChannelManager channelManager)
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
                _channelManager.SetPassword(channelName, character, password);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}