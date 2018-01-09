using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers
{
    public class StackSelection : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var selection = request.Data.GetOrDefault<long[]>(k.eid);
                var sourceContainerEid = request.Data.GetOrDefault<long>(k.container);
                var character = request.Session.Character;

                //load the container
                var container = Container.GetWithItems(sourceContainerEid, character, ContainerAccess.Remove);
                container.GetItems(selection).StackMany();
                container.Save();

                var result = container.ToDictionary();
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}