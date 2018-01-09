using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Applications;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationAcceptApplication : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var recruiterMember = request.Session.Character;
                var newMember = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
                var corporationEid = request.Data.GetOrDefault<long>(k.corporationEID);

                var corporation = recruiterMember.GetPrivateCorporationOrThrow();
                corporation.IsAnyRole(recruiterMember, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
                corporation.Eid.ThrowIfNotEqual(corporationEid, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                newMember.GetCorporationApplications().Any(a => a.CorporationEID == corporation.Eid).ThrowIfFalse(ErrorCodes.CorporationAppliacationNotFound);
                corporation.AddRecruitedMember(newMember, recruiterMember);

                var result = corporation.GetApplications().ToDictionary();
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}