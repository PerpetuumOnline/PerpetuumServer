using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationCharacterInvite : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationCharacterInvite(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            var issuerCharacter = request.Session.Character;
            var newMember = Character.Get(request.Data.GetOrDefault<int>(k.memberID));

            issuerCharacter.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);
            newMember.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);
            issuerCharacter.ThrowIfEqual(newMember, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            //one invite per character
            _corporationManager.Invites.ContainsInvite(newMember).ThrowIfTrue(ErrorCodes.CharacterIsInInviteProgress);

            //is he online?
            newMember.IsOnline.ThrowIfFalse(ErrorCodes.CharacterNotOnline);

            _corporationManager.IsJoinAllowed(newMember).ThrowIfFalse(ErrorCodes.CorporationChangeTooOften);

            var corporation = issuerCharacter.GetCorporation();
            corporation.IsActive.ThrowIfFalse(ErrorCodes.AccessDenied);
            corporation.IsAnyRole(issuerCharacter, CorporationRole.CEO, CorporationRole.HRManager, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.AccessDenied);
            corporation.IsAvailableFreeSlot.ThrowIfFalse(ErrorCodes.CorporationMaxMembersReached);

            _corporationManager.IsInLeavePeriod(newMember).ThrowIfTrue(ErrorCodes.CorporationMemberInLeavePeriod);
            _corporationManager.IsJoinPeriodExpired(newMember, corporation.Eid).ThrowIfFalse(ErrorCodes.CorporationCharacterInJoinPeriod);

            //invited member check
            var targetCharactersCorporation = newMember.GetCorporation().ThrowIfNotType<DefaultCorporation>(ErrorCodes.CharacterMustBeInDefaultCorporation);
            targetCharactersCorporation.IsActive.ThrowIfFalse(ErrorCodes.AccessDenied);

            Corporation.GetRoleFromSql(newMember).ThrowIfNotEqual(CorporationRole.NotDefined, ErrorCodes.InvitedMemberHasRoles);

            _corporationManager.Invites.AddInvite(issuerCharacter, newMember);

            var message = request.Data.GetOrDefault<string>(k.message);
            Message.Builder.SetCommand(Commands.CorporationCharacterInvite)
                .WithData(new Dictionary<string, object> { { k.characterID, issuerCharacter.Id }, { k.corporationEID, corporation.Eid }, { k.message, message } })
                .ToCharacter(newMember)
                .Send();

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}