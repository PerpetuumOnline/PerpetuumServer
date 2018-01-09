using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.RequestHandlers.TransportAssignments
{
    /// <summary>
    /// A volunteer takes a transport assignment
    /// </summary>
    public class TransportAssignmentTake : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;
                TakeTransportAssignment(id, character);
                
                scope.Complete();
            }
        }

        /// <summary>
        /// A volunteer takes a transport assignment
        /// </summary>
        private static void TakeTransportAssignment(int id, Character character)
        {
            var transportAssignmentInfo = TransportAssignment.Get(id);

            transportAssignmentInfo.ownercharacter.ThrowIfEqual(character, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            transportAssignmentInfo.taken.ThrowIfTrue(ErrorCodes.TransportAssignmentIsTaken);
            transportAssignmentInfo.volunteercharacter.ThrowIfNotEqual(null, ErrorCodes.TransportAssignmentIsTaken);

            transportAssignmentInfo.TakeCollateral(character);
            transportAssignmentInfo.expiry.ThrowIfLess(DateTime.Now, ErrorCodes.TransportAssignmentExpired);
            transportAssignmentInfo.volunteercharacter = character;
            transportAssignmentInfo.taken = true;
            transportAssignmentInfo.started = DateTime.Now;
            transportAssignmentInfo.UpdateToDb();

            VolumeWrapperContainer volumeWrapperContainer;
            PublicContainer publicContainer;
            transportAssignmentInfo.GiveToVolunteer(out volumeWrapperContainer, out publicContainer);

            transportAssignmentInfo.WriteLog(TransportAssignmentEvent.take, publicContainer.Parent);

            var extraInfo = new Dictionary<string, object>
            {
                {k.assignment, transportAssignmentInfo.ToDictionary()}
            };

            var assignmentInfo = new Dictionary<string, object>
            {
                {k.assignment, transportAssignmentInfo.ToPrivateDictionary()}
            };

            //send info to volunteer
            TransportAssignment.SendCommandWithTransportAssignmentsAndContainer(Commands.TransportAssignmentTake, publicContainer, character, assignmentInfo);

            //send info to principal
            Message.Builder.SetCommand(Commands.TransportAssignmentAccepted).WithData(extraInfo).ToCharacter(transportAssignmentInfo.ownercharacter).Send();
        }
    }
}