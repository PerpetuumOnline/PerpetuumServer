using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Lists my running assignments - taken or submitted
    /// </summary>
    public class TransportAssignmentRunning : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var info = TransportAssignment.GetRunningTransportAssignments(character).ToDictionary(true, character);

            var result = new Dictionary<string, object>
            {
                {k.transportAssignments, info},
                {k.count, TransportAssignment.GetCountInfo(character)}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}