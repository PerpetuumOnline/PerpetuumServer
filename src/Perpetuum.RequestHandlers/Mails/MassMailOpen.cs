using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MassMailOpen : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var mailId = request.Data.GetOrDefault<long>(k.ID);

                var mail = MassMailer.OpenMail(character, mailId).ThrowIfNull(ErrorCodes.MailNotFound);
                Message.Builder.FromRequest(request)
                    .WithData(mail.ToDetailedDictionary())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}