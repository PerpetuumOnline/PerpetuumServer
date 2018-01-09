using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    public class TransportAssignmentDeliver : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var wrapperEid = request.Data.GetOrDefault<long>(k.eid);
                var character = request.Session.Character;

                TransportAssignment.ManualDeliverTransportAssignment(character, wrapperEid);
                
                scope.Complete();
            }
        }
    }
}
