using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.ProximityProbes;

namespace Perpetuum.RequestHandlers
{
    public class ProximityProbeSetName : IRequestHandler
    {
        private readonly UnitHelper _unitHelper;

        public ProximityProbeSetName(UnitHelper unitHelper)
        {
            _unitHelper = unitHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var probeEid = request.Data.GetOrDefault<long>(k.eid);
                var probeName = request.Data.GetOrDefault<string>(k.name);
                var character = request.Session.Character;

                var probe = _unitHelper.GetUnitOrThrow<ProximityProbeBase>(probeEid);
                probe.HasAccess(character).ThrowIfError();
                probe.Name = probeName;
                probe.Save();

                Transaction.Current.OnCommited(() =>
                {
                    probe.ReloadRegistration();
                    probe.SendUpdateToAllPossibleMembers();
                });
                
                scope.Complete();
            }
        }
    }
}