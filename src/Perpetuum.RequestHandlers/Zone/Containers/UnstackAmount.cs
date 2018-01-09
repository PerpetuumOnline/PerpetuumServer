using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class UnstackAmount : ZoneContainerRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var itemEid = request.Data.GetOrDefault<long>(k.eid);
                var amount = request.Data.GetOrDefault<int>(k.amount);
                var size = request.Data.GetOrDefault<int>(k.size);

                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                var sourceContainerEid = request.Data.GetOrDefault<long>(k.container);
                var sourceContainer = request.Zone.FindContainerOrThrow(player, sourceContainerEid);
                CheckContainerType(sourceContainer).ThrowIfError();
                CheckFieldTerminalRange(player, sourceContainer).ThrowIfError();

                var targetContainerEid = request.Data.GetOrDefault<long>(k.targetContainer);
                var targetContainer = request.Zone.FindContainerOrThrow(player, targetContainerEid);
                CheckContainerType(targetContainer).ThrowIfError();
                CheckFieldTerminalRange(player, targetContainer).ThrowIfError();

                sourceContainer.EnlistTransaction();
                if (sourceContainer != targetContainer)
                    targetContainer.EnlistTransaction();


                sourceContainer.UnstackItem(itemEid, character, amount, size, targetContainer);
                sourceContainer.Save();
                targetContainer.Save();

                Transaction.Current.OnCompleted(c =>
                {
                    var result = new Dictionary<string, object>
                    {
                        { k.source, sourceContainer.ToDictionary() },
                        { k.target, targetContainer.ToDictionary() }
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}