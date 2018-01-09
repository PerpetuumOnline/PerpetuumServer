using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetBaseDeconstruct : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var state = request.Data.GetOrDefault<int>(k.state) == 1;
                var dockingBase = request.Zone.GetUnitOrThrow<PBSDockingBase>(eid);

                var character = request.Session.Character;
                dockingBase.SetDeconstructionRight(character,state).ThrowIfError();
                Transaction.Current.OnCommited(() => dockingBase.SendNodeUpdate());
                
                scope.Complete();
            }
        }
    }
}