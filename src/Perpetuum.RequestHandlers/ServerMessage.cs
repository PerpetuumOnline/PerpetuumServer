using System.Collections.Generic;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ServerMessage : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            Message.Builder.SetCommand(Commands.ServerMessage)
                .WithData(new Dictionary<string, object>(request.Data)).ToOnlineCharacters()
                .Send();
        }
    }
}