using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MassMailList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var folder = request.Data.GetOrDefault<int>(k.folder);

            var result = MassMailer.ListFolder(character, folder).ToDictionary("m", m => m.ToSimpleDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}