using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDropRoles : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var corporationEID = character.CorporationEid;
                var resultRole = CorporationRole.NotDefined;

                var corporation = Corporation.GetOrThrow(corporationEID);

                var role = corporation.GetMemberRole(character);

                if (role.IsAnyRole(CorporationRole.CEO))
                {
                    resultRole = resultRole | CorporationRole.CEO; //force add ceo role
                }


                //not ceo, not boardmember -> drop role is permitted                    
                corporation.SetMemberRole(character, resultRole);
                corporation.WriteRoleHistory(character, character, resultRole, role);
                CorporationData.RemoveFromCache(corporation.Eid);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}