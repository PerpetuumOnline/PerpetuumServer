using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sessions;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterDeselect : IRequestHandler
    {
        private readonly ISessionManager _sessionManager;

        public CharacterDeselect(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var session = _sessionManager.GetByCharacter(character);
                session?.DeselectCharacter();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}