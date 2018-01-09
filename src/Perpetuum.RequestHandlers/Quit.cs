using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class Quit : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            request.Session.ForceQuit(ErrorCodes.ServerDisconnects);
        }
    }
}