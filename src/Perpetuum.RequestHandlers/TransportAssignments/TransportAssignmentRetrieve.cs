using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Tries to retrieve an expired transport assignment. If it's unsuccessful it marks the assignment retrieved and it will be taken back at the next login.
    /// </summary>
    public class TransportAssignmentRetrieve : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;
                TransportAssignment.RetrieveTransportAssignment(id, character);
                
                scope.Complete();
            }
        }
    }
}