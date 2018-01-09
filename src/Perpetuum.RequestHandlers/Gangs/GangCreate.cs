using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Gangs
{
    public class GangCreate : IRequestHandler
    {
        private readonly IGangManager _gangManager;

        public GangCreate(IGangManager gangManager)
        {
            _gangManager = gangManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var gangName = request.Data.GetOrDefault<string>(k.name);
                var memberId = request.Data.GetOrDefault<int>(k.characterID);

                var currentGang = _gangManager.GetGangByMember(character);
                _gangManager.RemoveMember(currentGang, character, false);

                var newGang = _gangManager.CreateGang(gangName, character);

                Transaction.Current.OnCommited(() =>
                {
                    var result = newGang.ToDictionary();
                    result.Add(k.characterID, memberId);
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}