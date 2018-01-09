using Perpetuum.Data;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangLeave : IRequestHandler
    {
        private readonly IGangManager _gangManager;

        public GangLeave(IGangManager gangManager)
        {
            _gangManager = gangManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var gang = _gangManager.GetGangByMember(character);
                if (gang == null)
                    throw new PerpetuumException(ErrorCodes.CharacterNotInGang);

                _gangManager.RemoveMember(gang, character, false);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}