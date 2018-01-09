using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarFolderSectionDelete : IRequestHandler
    {
        private readonly IEntityRepository _entityRepository;

        public CorporationHangarFolderSectionDelete(IEntityRepository entityRepository)
        {
            _entityRepository = entityRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var hangarEID = request.Data.GetOrDefault<long>(k.container);
                var folderEID = request.Data.GetOrDefault<long>(k.eid);

                var corporateHangar = character.GetCorporation().GetHangar(hangarEID, character);
                corporateHangar.ReloadItems(character);

                var corporateHangarFolder = corporateHangar.GetItemOrThrow(folderEID);
                _entityRepository.Delete(corporateHangarFolder);

                Message.Builder.FromRequest(request)
                    .WithData(corporateHangar.ToDictionary())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}