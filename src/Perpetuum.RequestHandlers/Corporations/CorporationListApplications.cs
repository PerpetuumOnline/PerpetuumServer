using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Corporations.Applications;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    //a character applies for membership

    public class CorporationListApplications : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var corporation = character.GetPrivateCorporationOrThrow();
            corporation.IsAnyRole(character, CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

            var result = corporation.GetApplications().ToDictionary();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}
