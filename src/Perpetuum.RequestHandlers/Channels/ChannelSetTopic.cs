using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelSetTopic : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelSetTopic(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var channelName = request.Data.GetOrDefault<string>(k.channel);
                var newTopic = request.Data.GetOrDefault<string>(k.topic);

                var character = request.Session.Character;
                _channelManager.SetTopic(channelName, character, newTopic);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}