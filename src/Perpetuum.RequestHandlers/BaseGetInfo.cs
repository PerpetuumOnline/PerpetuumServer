using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers
{
    /// <summary>
    /// More info about a docking base
    /// Sent after a successful dock in
    /// </summary>
    public class BaseGetInfo : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public BaseGetInfo(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var baseEid = request.Data.GetOrDefault<long>(k.baseEID);
            var dockingBase = _dockingBaseHelper.GetDockingBase(baseEid);
            if (dockingBase is PBSDockingBase pbsDockingBase)
            {
                //after successful login this is just a check... maybe useless
                var issuerCharacter = request.Session.Character;
                pbsDockingBase.IsDockingAllowed(issuerCharacter).ThrowIfError();
            }

            var dockingBaseInfo = dockingBase?.ToDictionary();
            Message.Builder.FromRequest(request).WithData(dockingBaseInfo).Send();
        }
    }
}