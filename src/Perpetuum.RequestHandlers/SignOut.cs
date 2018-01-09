using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class SignOut : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var session = request.Session;
                session.SignOut();
                session.SendMessage(Message.Builder.FromRequest(request).WithOk());
                scope.Complete();
            }
        }
    }

}