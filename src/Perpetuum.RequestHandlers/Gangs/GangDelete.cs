using Perpetuum.Data;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangDelete : IRequestHandler
    {
        private readonly IGangManager _gangManager;
        private readonly IGangInviteService _gangInviteService;

        public GangDelete(IGangManager gangManager,IGangInviteService gangInviteService)
        {
            _gangManager = gangManager;
            _gangInviteService = gangInviteService;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var gang = _gangManager.GetGangByMember(character);
                if (gang.Leader != character)
                    throw new PerpetuumException(ErrorCodes.OnlyGangLeaderCanDoThis);
                _gangManager.DisbandGang(gang);

                _gangInviteService.RemoveInvitesByGang(gang);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}