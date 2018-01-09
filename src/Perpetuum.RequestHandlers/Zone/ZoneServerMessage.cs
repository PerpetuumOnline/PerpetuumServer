using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneServerMessage : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var messageBuilder = Message.Builder.SetCommand(Commands.ServerMessage).WithData(new Dictionary<string, object>(request.Data));
            request.Zone.SendMessageToPlayers(messageBuilder);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}
