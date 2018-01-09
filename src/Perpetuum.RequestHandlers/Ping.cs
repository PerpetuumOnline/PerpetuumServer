using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class Ping : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            //if it's requested then it means the client pings the relay
            //if not then it's a reply to the relay's ping
            if (!request.Data.ContainsKey(k.state))
                return;
            
            request.Data[k.state] = "response";
            Message.Builder.FromRequest(request).WithData(request.Data).Send();
        }
    }
}