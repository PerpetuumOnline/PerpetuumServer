using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AllianceRoleHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithEmpty().Send();
        }
    }
}