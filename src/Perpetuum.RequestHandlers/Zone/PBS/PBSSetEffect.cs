using System.Transactions;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.EffectNodes;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetEffect : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var targetEffectType = (EffectType) request.Data.GetOrDefault<int>(k.effect);

                var effectNode = request.Zone.GetUnit(eid).ThrowIfNotType<PBSEffectNode>(ErrorCodes.ItemNotFound);

                effectNode.CheckAccessAndThrowIfFailed(character);
                effectNode.IsFullyConstructed().ThrowIfFalse(ErrorCodes.ObjectNotFullyConstructed);
                effectNode.OnlineStatus.ThrowIfFalse(ErrorCodes.NodeOffline);
                effectNode.AvailableEffects.Length.ThrowIfEqual(1, ErrorCodes.DefinitionNotSupported);
                effectNode.CurrentEffectType = targetEffectType;
                effectNode.Save();

                Transaction.Current.OnCommited(() => effectNode.SendNodeUpdate());
                scope.Complete();
            }
        }
    }
}