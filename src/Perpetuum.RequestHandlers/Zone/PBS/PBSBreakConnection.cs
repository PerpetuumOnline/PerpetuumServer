using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSBreakConnection : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.source);
                var targetEid = request.Data.GetOrDefault<long>(k.target);
                var character = request.Session.Character;

                var sourceUnit = request.Zone.GetUnitOrThrow(sourceEid);
                var targetUnit = request.Zone.GetUnitOrThrow(targetEid);

                var sourceNode = sourceUnit.ThrowIfNotType<IPBSObject>(ErrorCodes.DefinitionNotSupported);
                sourceNode.ConnectionHandler.NetworkNodes.Any(n => n.IsReinforced()).ThrowIfTrue(ErrorCodes.NetworkHasReinforcedNode);

                var targetNode = targetUnit.ThrowIfNotType<IPBSObject>(ErrorCodes.DefinitionNotSupported);
                sourceNode.ConnectionHandler.BreakConnection(targetNode, character);

                Transaction.Current.OnCommited(() =>
                {
                    sourceNode.SendNodeUpdate();
                    PBSHelper.WritePBSLog(PBSLogType.disconnected, sourceUnit.Eid, sourceUnit.Definition, sourceUnit.Owner, character.Id, otherNodeEid: targetUnit.Eid, otherNodeDefinition: targetUnit.Definition, zoneId: request.Zone.Id);
                });

                Message.Builder.FromRequest(request).WithOk().Send();
                scope.Complete();
            }
        }
    }
}