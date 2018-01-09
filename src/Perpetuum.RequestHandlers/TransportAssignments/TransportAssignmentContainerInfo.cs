using Perpetuum.Containers;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Returns the transport assignment info based on the VolumeWrapperContainer
    /// </summary>
    public class TransportAssignmentContainerInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var eid = request.Data.GetOrDefault<long>(k.eid);

            var volumeWrapperContainer = (VolumeWrapperContainer) Container.GetOrThrow(eid);
            var character = request.Session.Character;
            volumeWrapperContainer.Owner.ThrowIfNotEqual(character.Eid,ErrorCodes.AccessDenied);

            var result = volumeWrapperContainer.GetAssignmentInfo();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}