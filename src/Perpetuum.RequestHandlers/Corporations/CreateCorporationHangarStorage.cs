using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers.Corporations
{
    /// <summary>
    /// Creates a system container on the base which holds the corporation hangars
    /// </summary>
    public class CreateCorporationHangarStorage : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public CreateCorporationHangarStorage(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var baseEid = request.Data.GetOrDefault<long>(k.baseEID);

                var dockingBase = _dockingBaseHelper.GetDockingBase(baseEid);

                var hangarStorage = dockingBase.GetPublicCorporationHangarStorage();
                if (hangarStorage == null)
                {
                    //hangar already exists
                    throw new PerpetuumException(ErrorCodes.ItemAlreadyExists);
                }

                var mainHangar = Entity.Factory.CreateWithRandomEID(DefinitionNames.PUBLIC_CORPORATE_HANGARS_STORAGE);
                mainHangar.Owner = dockingBase.Owner;
                mainHangar.Parent = baseEid;
                mainHangar.Save();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}