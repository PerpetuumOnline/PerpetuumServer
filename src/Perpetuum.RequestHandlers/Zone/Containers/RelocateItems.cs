using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class RelocateItems : ZoneContainerRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                var sourceContainerEid = request.Data.GetOrDefault<long>(k.sourceContainer);
                var sourceContainer = request.Zone.FindContainer(player, sourceContainerEid).ThrowIfNull(ErrorCodes.ContainerNotFound);
                CheckContainerType(sourceContainer, character.Id).ThrowIfError();
                CheckFieldTerminalRange(player, sourceContainer).ThrowIfError();

                var targetContainerEid = request.Data.GetOrDefault<long>(k.targetContainer);
                var targetContainer = request.Zone.FindContainer(player, targetContainerEid).ThrowIfNull(ErrorCodes.ContainerNotFound);
                CheckContainerType(targetContainer, character.Id).ThrowIfError();
                CheckFieldTerminalRange(player, targetContainer).ThrowIfError();

                sourceContainer.EnlistTransaction();
                if (sourceContainer != targetContainer)
                    targetContainer.EnlistTransaction();

                var itemEids = request.Data.GetOrDefault<long[]>(k.eid);
                sourceContainer.RelocateItems(character, character, itemEids, targetContainer);

                sourceContainer.Save();
                targetContainer.Save();

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.source, sourceContainer.ToDictionary()},
                        {k.target, targetContainer.ToDictionary()}
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

       
    }
}