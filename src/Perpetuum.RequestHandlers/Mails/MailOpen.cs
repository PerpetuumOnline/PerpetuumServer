using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailOpen : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var mailId = request.Data.GetOrDefault<string>(k.ID);
                var character = request.Session.Character;
                var mail = MailHandler.OpenMail(character, mailId);
                Message.Builder.FromRequest(request).WithData(mail.toDictionary()).WrapToResult().Send();
                
                scope.Complete();
            }
        }
    }
}