using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSMakeConnection : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.source);
                var targetEid = request.Data.GetOrDefault<long>(k.target);
                var character = request.Session.Character;

                var sourceUnit = request.Zone.GetUnitOrThrow(sourceEid);
                var targetUnit = request.Zone.GetUnitOrThrow(targetEid);;

                if (sourceUnit is IPBSObject sourceNode && targetUnit is IPBSObject targetNode)
                {
                    try
                    {
                        sourceNode.ConnectionHandler.MakeConnection(targetNode, character);
                        PBSHelper.WritePBSLog(PBSLogType.connected, sourceUnit.Eid, sourceUnit.Definition,
                            sourceUnit.Owner, character.Id, otherNodeEid: targetUnit.Eid,
                            otherNodeDefinition: targetUnit.Definition, zoneId: request.Zone.Id, background: false);

                        Transaction.Current.OnCommited(() => sourceNode.SendNodeUpdate());
                    }
                    catch (PerpetuumException gex)
                    {
                        if (gex.error != ErrorCodes.DockingBaseExistsInNetwork)
                            throw;

                        foreach (var node in targetNode.ConnectionHandler.NetworkNodes.OfType<Unit>())
                        {
                            node.Owner = targetUnit.Owner;
                        }

                        throw;
                    }
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}