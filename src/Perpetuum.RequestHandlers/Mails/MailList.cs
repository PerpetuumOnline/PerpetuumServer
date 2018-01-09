using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var folder = request.Data.GetOrDefault<int>(k.folder);
            var character = request.Session.Character;

            var result = MailHandler.ListMails(character, folder).ToDictionary("m", m => m.toDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}