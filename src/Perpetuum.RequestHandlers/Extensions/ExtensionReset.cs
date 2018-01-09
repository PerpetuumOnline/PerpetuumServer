using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionReset : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.ResetAllExtensions();
                Message.Builder.FromRequest(request).WithOk().Send();
                Logger.Info("extension was reset for character: " + character.Id);
                
                scope.Complete();
            }
        }
    }
}