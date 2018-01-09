using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelList : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelList(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = _channelManager.GetPublicChannels().ToDictionary("c", c => c.ToDictionary(character, false));
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}