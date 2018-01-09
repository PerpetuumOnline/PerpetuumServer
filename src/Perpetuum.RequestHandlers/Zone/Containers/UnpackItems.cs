using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class UnpackItems : ZoneContainerRequestHandler
    {
        private readonly IEntityRepository _entityRepository;

        public UnpackItems(IEntityRepository entityRepository)
        {
            _entityRepository = entityRepository;
        }

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

                var targets = request.Data.GetOrDefault<long[]>(k.target);

                container.EnlistTransaction();
                container.GetItems(targets).UnpackMany();
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