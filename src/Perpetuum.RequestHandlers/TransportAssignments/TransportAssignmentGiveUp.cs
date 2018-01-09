using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// The volunteer gives up an assignment
    /// </summary>
    public class TransportAssignmentGiveUp : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var assignmentId = request.Data.GetOrDefault<int>(k.ID);

                var info = TransportAssignment.Get(assignmentId);
                var character = request.Session.Character;
                info.GiveUpAssignment(character);
                
                scope.Complete();
            }
        }
    }

}
