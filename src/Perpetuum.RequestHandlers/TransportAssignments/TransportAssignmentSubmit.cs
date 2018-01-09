using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Starts a transport assignment with the given VolumeWrapperContainer
    /// </summary>
    public class TransportAssignmentSubmit : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var wrapperContainerEid = request.Data.GetOrDefault<long>(k.eid);
                var reward = request.Data.GetOrDefault<long>(k.reward);
                var collateral = request.Data.GetOrDefault<long>(k.collateral);
                var sourceBaseEid = request.Data.GetOrDefault<long>(k.sourceBase);
                var targetBaseEid = request.Data.GetOrDefault<long>(k.targetBase);
                var durationDays = request.Data.GetOrDefault<int>(k.duration);

                var character = request.Session.Character;
                TransportAssignment.SubmitTransportAssignment(character, wrapperContainerEid, reward, collateral, sourceBaseEid, targetBaseEid, durationDays);
                
                scope.Complete();
            }
        }
    }
}