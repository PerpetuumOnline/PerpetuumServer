﻿using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelListAll : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelListAll(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = _channelManager.GetAllChannels().ToDictionary("c", c => c.ToDictionary(character, false));
            Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
        }
    }
}