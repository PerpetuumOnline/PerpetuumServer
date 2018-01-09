using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// Lists the content of a VolumeWrapperContainer
    /// </summary>
    public class TransportAssignmentListContent : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var assignmentId = request.Data.GetOrDefault<int>(k.ID);

            var info = TransportAssignment.Get(assignmentId);

            var character = request.Session.Character;
            info.ownercharacter.ThrowIfNotEqual(character,ErrorCodes.AccessDenied);

            var volumeWrapperContainer = info.GetContainer();

            //depending on the owner status a different character must be used

            var ownerCharacter = info.taken ? info.volunteercharacter : character;

            volumeWrapperContainer.ReloadItems(ownerCharacter);

            var containerInfo = volumeWrapperContainer.ToDictionary();
            var result = new Dictionary<string, object>
            {
                {k.container, containerInfo},
                {k.assignment, info.ToDictionary()}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}