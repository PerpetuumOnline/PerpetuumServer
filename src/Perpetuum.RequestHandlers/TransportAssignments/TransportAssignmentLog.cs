using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Lists the transport assignments log for a certain period
    /// </summary>
    public class TransportAssignmentLog : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var offset = request.Data.GetOrDefault<int>(k.offset);

            var character = request.Session.Character;
            var info = TransportAssignment.TransportAssignmentHistory(offset, character.Id);

            var result = new Dictionary<string, object>
            {
                {k.history, info}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }

    }
}