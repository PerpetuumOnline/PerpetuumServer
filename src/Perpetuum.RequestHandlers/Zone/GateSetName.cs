using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Gates;

namespace Perpetuum.RequestHandlers.Zone
{
    public class GateSetName : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var name = request.Data.GetOrDefault<string>(k.name);
                var eid = request.Data.GetOrDefault<long>(k.eid);
            
                var gate =  request.Zone.GetUnitOrThrow(eid) as Gate;
                if (gate == null)
                    throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                var character = request.Session.Character;
                gate.Rename(character, name);

                Transaction.Current.OnCommited(()=>Message.Builder.FromRequest(request).WithData(gate.ToDictionary()).Send());
                
                scope.Complete();
            }
        }
    }
}
