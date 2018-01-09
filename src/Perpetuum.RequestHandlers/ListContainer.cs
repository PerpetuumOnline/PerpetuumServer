using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ListContainer : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var containerEid = request.Data.GetOrDefault<long>(k.container);

            Dictionary<string, object> result;
            Container container = null;

            try
            {
                container = Container.GetWithItems(containerEid, request.Session.Character, ContainerAccess.List);
                result = container.ToDictionary();
            }
            catch (PerpetuumException gex)
            {
                if (container != null && gex.error == ErrorCodes.InsufficientPrivileges)
                {
                    result = container.ToDictionary();
                    result.Remove(k.items);
                }
                else
                {
                    result = new Dictionary<string, object>
                    {
                        {k.rErr, (int) gex.error},
                        {k.containerEID, containerEid}
                    };
                }
            }

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}