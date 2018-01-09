using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Lists the advertised transport assignments
    /// </summary>
    public class TransportAssignmentList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var info = TransportAssignment.GetAdvertisedTransportAssignments(character).ToDictionary(false, null);

            var result = new Dictionary<string, object>
            {
                {k.transportAssignments, info},
                {k.count, TransportAssignment.GetCountInfo(character)}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}