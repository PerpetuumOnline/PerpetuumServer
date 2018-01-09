using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class TrashItems : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var target = request.Data.GetOrDefault<long[]>(k.target);
                var sourceContainerEid = request.Data.GetOrDefault<long>(k.container);

                var container = Container.GetWithItems(sourceContainerEid, character, ContainerAccess.Remove);
                container.TrashItems(character, target);
                container.Save();

                Message.Builder.FromRequest(request)
                    .WithData(container.ToDictionary())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}