using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers
{
    public class ItemShopList : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public ItemShopList(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var inBaseEid = request.Data.GetOrDefault<long>(k.baseEID);

            var dockingBase = _dockingBaseHelper.GetDockingBase(inBaseEid == 0 ? character.CurrentDockingBaseEid : inBaseEid);
            if (dockingBase == null)
                throw new PerpetuumException(ErrorCodes.DockingBaseNotFound);

            var shop = dockingBase.GetItemShop();
            var result = shop.EntriesToDictionary();

            Message.Builder.FromRequest(request)
                .WithData(new Dictionary<string, object>{ {k.shop, result},})
                .Send();
        }
    }
}