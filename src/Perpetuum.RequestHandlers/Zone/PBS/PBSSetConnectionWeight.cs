using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetConnectionWeight : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.source);
                var character = request.Session.Character;
                var weight = request.Data.GetOrDefault<double>(k.weight);
                var targetEid = request.Data.GetOrDefault<long>(k.target);

                var sourceUnit = request.Zone.GetUnitOrThrow(sourceEid);
                var sourceNode = sourceUnit.ThrowIfNotType<IPBSObject>(ErrorCodes.DefinitionNotSupported);

                var targetUnit = request.Zone.GetUnitOrThrow(targetEid);
                var targetNode = targetUnit.ThrowIfNotType<IPBSObject>(ErrorCodes.DefinitionNotSupported);

                sourceNode.CheckAccessAndThrowIfFailed(character);
                sourceNode.ConnectionHandler.SetWeight(targetNode, weight);
                sourceUnit.Save();

                Transaction.Current.OnCommited(() => sourceNode.SendNodeUpdate());

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}