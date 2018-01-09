using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations.YellowPages
{
    public class YellowPagesDelete : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public YellowPagesDelete(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var corporationeid = character.CorporationEid;
                DefaultCorporationDataCache.IsCorporationDefault(corporationeid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);

                var role = Corporation.GetRoleFromSql(character);
                role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.HRManager, CorporationRole.PRManager).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                //do the work
                _corporationManager.DeleteYellowPages(corporationeid);

                var entry = _corporationManager.GetYellowPages(corporationeid);
                var result = new Dictionary<string, object> { { k.data, entry } };
                Message.Builder.FromRequest(request).WithData(result).Send();
                CorporationData.RemoveFromCache(corporationeid);
                
                scope.Complete();
            }
        }
    }
}