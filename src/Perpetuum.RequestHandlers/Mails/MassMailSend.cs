using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;
using Perpetuum.Services.Social;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MassMailSend : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var targets = request.Data.GetOrDefault<int[]>(k.target).ToCharacter().ToArray();
                var subject = request.Data.GetOrDefault<string>(k.subject);
                var body = request.Data.GetOrDefault<string>(k.body);

                var filteredTargets = character.FilterWhoBlockedMe(targets).ToArray();

                filteredTargets.Length.ThrowIfEqual(0, ErrorCodes.TargetBlockedTheRequest);

                var mail = new MassMail
                {
                    body = body,
                    subject = subject,
                    sender = character,
                    folder = MailFolder.inbox,
                    targets = filteredTargets,
                    type = MailType.character
                };

                //write to targets' inboxes
                MassMailer.WriteMailToTargets(mail).ThrowIfError();

                //write to sender's inbox
                MassMailer.WriteToOutbox(mail).ThrowIfError();

                var result = MassMailer.ListFolder(character, (int)MailFolder.outbox).ToDictionary("m", m => m.ToSimpleDictionary());
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}