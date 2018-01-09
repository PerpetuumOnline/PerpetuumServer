using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AllianceGetMyInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithEmpty().Send();
        }
    }
}