using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers
{
    public class BaseGetMyItems : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public BaseGetMyItems(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var baseEid = request.Data.GetOrDefault<long>(k.baseEID);
            var dockingBase = _dockingBaseHelper.GetDockingBase(baseEid);
            if (dockingBase == null)
                throw new PerpetuumException(ErrorCodes.DockingBaseNotFound);

            var publicContainer = dockingBase.GetPublicContainerWithItems(request.Session.Character);
            var result = publicContainer.ToDictionary();
            result.Add(k.baseEID, baseEid);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}