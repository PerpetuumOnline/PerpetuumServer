using System.Linq;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarFolderSectionCreate : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationHangarFolderSectionCreate(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var hangarEID = request.Data.GetOrDefault<long>(k.eid);

                var corporation = character.GetCorporation();
                var corporateHangar = corporation.GetHangar(hangarEID, character);

                corporateHangar.Folders.Count().ThrowIfGreaterOrEqual(_corporationManager.Settings.NumberOfHangarFolders, ErrorCodes.MaxNumberOfHangarFoldersReached);

                var corporateHangarFolder = CorporateHangarFolder.CreateCorporateHangarFolder();
                corporateHangarFolder.Owner = corporation.Eid;

                corporateHangar.AddChild(corporateHangarFolder);

                //inherit logging
                corporateHangarFolder.SetLogging(corporateHangar.IsLogging(), null);
                corporateHangar.Save();

                Message.Builder.FromRequest(request)
                    .WithData(corporateHangarFolder.ToDictionary())
                    .Send();
                scope.Complete();
            }
        }
    }
}