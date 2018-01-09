using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarSetAccess : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var hangarEID = request.Data.GetOrDefault<long>(k.eid);
                var hangarAccess = request.Data.GetOrDefault<int>(k.hangarAccess);

                hangarAccess = (int)((CorporationRole)(hangarAccess & (int)PresetCorporationRoles.HANGAR_ACCESS_MASK)).GetHighestContainerAccess();

                if (hangarAccess == 0)
                {
                    hangarAccess = (int)CorporationRole.HangarAccess_low;
                }

                hangarAccess = (int)((CorporationRole)hangarAccess).GetHighestContainerAccess();
                var removeRole = ((CorporationRole)hangarAccess).GetRelatedRemoveAccess();

                var corporation = character.GetCorporation();
                var corporateHangar = corporation.GetHangar(hangarEID, character, ContainerAccess.LogClear);

                var role = corporation.GetMemberRole(character);

                //has the member access to the new role?
                if (!role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO))
                {
                    role.IsAnyRole(removeRole).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
                }

                corporateHangar.SetHangarAccess(character, (CorporationRole)hangarAccess);
                corporateHangar.Save();

                Message.Builder.FromRequest(request).WithData(corporateHangar.ToDictionary()).Send();
                
                scope.Complete();
            }
        }
    }
}