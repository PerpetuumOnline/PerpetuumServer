using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailNewCount : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = new Dictionary<string, object>
            {
                {k.amount, MailHandler.NewMailCount(character)}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}