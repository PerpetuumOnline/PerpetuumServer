using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MailSend : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var target = Character.Get(request.Data.GetOrDefault<int>(k.target));
            var subject = request.Data.GetOrDefault<string>(k.subject);
            var body = request.Data.GetOrDefault<string>(k.body);

            character.ThrowIfEqual(target, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            target.IsBlocked(character).ThrowIfTrue(ErrorCodes.TargetBlockedTheRequest);

            const MailType mailType = MailType.character;

            MailHandler.SendMail(character, target, subject, body, mailType, out _, out var sourceID).ThrowIfError();
            Message.Builder.FromRequest(request).WithData(sourceID.ToString()).WrapToResult().Send();
        }
    }
}