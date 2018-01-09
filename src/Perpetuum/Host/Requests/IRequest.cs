using System.Collections.Generic;
using Perpetuum.Services.Sessions;
using Perpetuum.Zones;

namespace Perpetuum.Host.Requests
{
    public interface IRequest
    {
        ISession Session { get; }
        Command Command { get; }
        string Target { get; }
        Dictionary<string, object> Data { get; }
    }

    public interface IZoneRequest : IRequest
    {
        IZone Zone { get; }
    }
}