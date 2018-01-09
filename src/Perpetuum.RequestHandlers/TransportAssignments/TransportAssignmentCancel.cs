using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// User cancels a pending assignment
    /// </summary>
    public class TransportAssignmentCancel : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;
                TransportAssignment.CancelWaitingTransportAssignment(id, character);
                
                scope.Complete();
            }
        }
    }
}