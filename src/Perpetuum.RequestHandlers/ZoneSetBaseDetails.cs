using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers
{
    public class ZoneSetBaseDetails : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public ZoneSetBaseDetails(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);

                var dockingBase = _dockingBaseHelper.GetDockingBase(eid);
                if (dockingBase == null)
                    throw new PerpetuumException(ErrorCodes.DockingBaseNotFound);

                if (request.Data.TryGetValue(k.welcome, out string welcome))
                {
                    dockingBase.DynamicProperties.Update(k.welcome, welcome);
                }

                if (request.Data.TryGetValue(k.name, out string name))
                {
                    dockingBase.Name = name;
                }

                dockingBase.Save();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}