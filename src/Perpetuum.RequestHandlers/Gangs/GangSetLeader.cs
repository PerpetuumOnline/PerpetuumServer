using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangSetLeader : IRequestHandler
    {
        private readonly IGangManager _gangManager;

        public GangSetLeader(IGangManager gangManager)
        {
            _gangManager = gangManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var member = Character.Get(request.Data.GetOrDefault<int>(k.memberID));

                var gang = _gangManager.GetGangByMember(character);
                if (gang == null)
                    throw new PerpetuumException(ErrorCodes.CharacterNotInGang);

                if (character != gang.Leader)
                    throw new PerpetuumException(ErrorCodes.OnlyGangLeaderCanDoThis);

                _gangManager.ChangeLeader(gang, member);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}