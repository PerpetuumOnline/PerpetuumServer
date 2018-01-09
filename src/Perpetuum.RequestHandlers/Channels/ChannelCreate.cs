using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Channels;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelCreate : IRequestHandler
    {
        private readonly IChannelManager _channelManager;

        public ChannelCreate(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var channelName = request.Data.GetOrDefault<string>(k.channel);

                _channelManager.GetChannelByName(channelName).ThrowIfNotNull(ErrorCodes.ChannelAlreadyExists);
                _channelManager.CreateChannel(ChannelType.Public, channelName);
                _channelManager.JoinChannel(channelName, character, PresetChannelRoles.ROLE_GOD, null);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}