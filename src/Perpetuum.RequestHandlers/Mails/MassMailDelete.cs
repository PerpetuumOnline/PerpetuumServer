using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Mail;

namespace Perpetuum.RequestHandlers.Mails
{
    public class MassMailDelete : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var mailIDs = request.Data.GetOrDefault<long[]>(k.ID);

                MassMailer.DeleteMail(character, mailIDs).ThrowIfError();

                var dictionary = new Dictionary<string, object>
                {
                    {k.ID, mailIDs}
                };

                Message.Builder.FromRequest(request)
                    .WithData(dictionary)
                    .Send();
                
                scope.Complete();
            }
        }
    }
}