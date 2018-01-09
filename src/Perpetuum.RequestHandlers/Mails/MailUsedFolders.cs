using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailUsedFolders : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var result = MailHandler.ListUsedFolders(character).ToArray();

            var builder = Message.Builder.FromRequest(request);
            if (result.Length == 0)
                builder.WithEmpty();
            else
                builder.WithData(result).WrapToResult();

            builder.Send();
        }
    }
}