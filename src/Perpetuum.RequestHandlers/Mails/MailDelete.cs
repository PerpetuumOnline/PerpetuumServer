using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailDelete : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var mailId = request.Data.GetOrDefault<string>(k.ID);
                var character = request.Session.Character;
                MailHandler.DeleteMail(character, mailId);
                Message.Builder.FromRequest(request).WithData(mailId).WrapToResult().Send();
                
                scope.Complete();
            }
        }
    }
}