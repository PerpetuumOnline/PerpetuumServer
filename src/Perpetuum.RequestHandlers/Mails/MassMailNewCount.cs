using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MassMailNewCount : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var mailCount = MassMailer.NewMassMailCount(character);

            var result = new Dictionary<string, object>
            {
                {k.amount, mailCount}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}