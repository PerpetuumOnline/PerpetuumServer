using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers
{
    public class UnpackItems : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var containerEid = request.Data.GetOrDefault<long>(k.container);
                var targets = request.Data.GetOrDefault<long[]>(k.target);

                var character = request.Session.Character;
                var container = Container.GetWithItems(containerEid, character, ContainerAccess.Remove);
                container.GetItems(targets).UnpackMany();
                container.Save();

                var result = container.ToDictionary();
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}