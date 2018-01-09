using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelGetBannedMembers : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelGetBannedMembers(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            var channelName = request.Data.GetOrDefault<string>(k.channel);
            var character = request.Session.Character;
            var bannedMembers = _channelManager.GetBannedCharacters(channelName, character).GetCharacterIDs().ToArray();
            var result = new Dictionary<string, object> { { k.members, bannedMembers } };
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}