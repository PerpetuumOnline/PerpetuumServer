using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelMyList : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelMyList(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = _channelManager.GetChannelsByMember(character).ToDictionary("c", c => c.ToDictionary(character, true));
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}