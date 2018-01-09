using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationLeave : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationLeave(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                character.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);

                var corporation = character.GetPrivateCorporationOrThrow();

                //last man can leave the corporation
                if (corporation.MembersCount != 1)
                {
                    corporation.GetMemberRole(character).IsAnyRole(CorporationRole.CEO).ThrowIfTrue(ErrorCodes.CorporationDropCEOFirst);
                }

                var ceo = corporation.CEO;
                if (ceo != character)
                {
                    _corporationManager.IsJoinPeriodExpired(character, corporation.Eid).ThrowIfFalse(ErrorCodes.CorporationCharacterInJoinPeriod);
                }

                _corporationManager.IsInLeavePeriod(character).ThrowIfTrue(ErrorCodes.CorporationMemberInLeavePeriod);

                //all good, drop the role
                if (corporation.MembersCount != 1)
                {
                    //drop the member's role
                    corporation.SetMemberRole(character, CorporationRole.NotDefined);
                }

                var leaveTime = _corporationManager.AddLeaveEntry(character);

                Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.leaveEnd, leaveTime } }).Send();
                
                scope.Complete();
            }
        }
    }
}