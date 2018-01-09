using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class SetItemName : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var target = request.Data.GetOrDefault<long>(k.target);
                var newName = request.Data.GetOrDefault<string>(k.name);
                var sourceContainerEid = request.Data.GetOrDefault<long>(k.container);

                var container = Container.GetWithItems(sourceContainerEid, character, ContainerAccess.Remove);
                container.SetItemName(target, newName);
                container.Save();

                Message.Builder.FromRequest(request)
                    .WithData(container.ToDictionary())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}