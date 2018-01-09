using System.Collections.Generic;
using Perpetuum.Services.Sessions;
using Perpetuum.Zones;

namespace Perpetuum.Host.Requests
{
    public class Request : IRequest
    {
        public ISession Session { get; set; }
        public Command Command { get; set; }
        public string Target { get; set; }
        public Dictionary<string,object> Data { get; set; } = new Dictionary<string, object>();
    }

    public class ZoneRequest : IZoneRequest
    {
        private readonly IRequest _request;

        public ZoneRequest(IRequest request)
        {
            _request = request;
        }

        public IZone Zone { get; set; }
        public ISession Session => _request.Session;
        public Command Command => _request.Command;
        public string Target => _request.Target;
        public Dictionary<string, object> Data => _request.Data;
    }
}