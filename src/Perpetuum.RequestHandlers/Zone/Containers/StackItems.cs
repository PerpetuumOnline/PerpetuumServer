using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class StackItems : ZoneContainerRequestHandler
    {
        public override void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                var containerEid = request.Data.GetOrDefault<long>(k.container);
                var container = request.Zone.FindContainerOrThrow(player, containerEid);

                CheckContainerType(container).ThrowIfError();
                CheckFieldTerminalRange(player, container).ThrowIfError();

                var selection = request.Data.GetOrDefault<long[]>(k.eid);

                container.EnlistTransaction();
                container.GetItems(selection).StackMany();
                container.Save();

                Transaction.Current.OnCompleted(c =>
                {
                    var result = container.ToDictionary();
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                scope.Complete();
            }
        }
    }
}