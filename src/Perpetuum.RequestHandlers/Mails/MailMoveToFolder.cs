using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailMoveToFolder : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var mailID = request.Data.GetOrDefault<string>(k.ID);
                var folder = request.Data.GetOrDefault<int>(k.folder);
                MailHandler.MoveToFolder(character, mailID, folder);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}