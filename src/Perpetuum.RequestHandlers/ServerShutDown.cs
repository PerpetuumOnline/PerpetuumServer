using System;
using Perpetuum.Host;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ServerShutDown : IRequestHandler
    {
        private readonly HostShutDownManager _shutDownManager;

        public ServerShutDown(HostShutDownManager shutDownManager)
        {
            _shutDownManager = shutDownManager;
        }

        public void HandleRequest(IRequest request)
        {
            var message = request.Data.GetOrDefault<string>(k.message);
            var time = request.Data.GetOrDefault<DateTime>(k.date);
            _shutDownManager.StartShutDown(request.Command, message, time);
        }
    }
}