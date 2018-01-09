using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarClose : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;

        public CorporationHangarClose(IEntityRepository entityRepository)
        {
            _entityRepository = entityRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var hangarEid = request.Data.GetOrDefault<long>(k.eid);

            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

            var corporateHangar = character.GetCorporation().GetHangar(hangarEid, character, ContainerAccess.LogList);
            corporateHangar.ReloadItems(corporateHangar.Owner);
            corporateHangar.IsLeaseExpired.ThrowIfTrue(ErrorCodes.CorporationHangarLeaseExpired);
            _entityRepository.Delete(corporateHangar);

            var result = new Dictionary<string, object> { { k.eid, hangarEid } };

            Message.Builder.FromRequest(request)
                .WithData(result)
                .Send();
        }
    }
}