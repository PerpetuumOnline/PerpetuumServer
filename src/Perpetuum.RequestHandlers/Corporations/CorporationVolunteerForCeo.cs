using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationVolunteerForCeo : IRequestHandler
    {
        private readonly IVolunteerCEOService _volunteerCEOService;

        public CorporationVolunteerForCeo(IVolunteerCEOService volunteerCEOService)
        {
            _volunteerCEOService = volunteerCEOService;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var corporation = character.GetPrivateCorporationOrThrow();

                var role = corporation.GetMemberRole(character);
                if (!role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO))
                    throw new PerpetuumException(ErrorCodes.InsufficientPrivileges);

                var volunteerCEO = _volunteerCEOService.GetVolunteer(corporation.Eid);
                if (volunteerCEO != null)
                {
                    _volunteerCEOService.ClearVolunteer(volunteerCEO);
                }
                else
                {
                    corporation.CheckMaxMemberCountAndThrowIfFailed(character);
                    corporation.CheckCeoLastActivityAndThrowIfFailed();
                    role.HasFlag(CorporationRole.CEO).ThrowIfTrue(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                    volunteerCEO = _volunteerCEOService.AddVolunteer(corporation,character);
                }

                Transaction.Current.OnCommited(() => _volunteerCEOService.SendVolunteerStatusToMembers(volunteerCEO));
                
                scope.Complete();
            }
        }
    }
}