using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    //szandekosan nincs access check, mindenki feedelheti
    public class PBSFeedItemsHander : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var targetEids = request.Data.GetOrDefault<long[]>(k.target);
                var feedableUnitEid = request.Data.GetOrDefault<long>(k.eid);

                var feedableUnit = request.Zone.GetUnitOrThrow(feedableUnitEid);
                var feedable = (feedableUnit as IPBSFeedable).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);
                player.CurrentPosition.IsInRangeOf3D(feedableUnit.CurrentPosition, DistanceConstants.PBS_NODE_USE_DISTANCE).ThrowIfFalse(ErrorCodes.TargetOutOfRange);

                feedable.FeedWithItems(player, targetEids);

                Transaction.Current.OnCompleted(c =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.info, feedableUnit.ToDictionary()},
                        {k.container, player.GetContainer().ToDictionary()}
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}
