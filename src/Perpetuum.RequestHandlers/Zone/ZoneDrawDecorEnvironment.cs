using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawDecorEnvironment : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var decorId = request.Data.GetOrDefault<int>(k.ID);
                request.Zone.DecorHandler.DrawDecorEnvironment(decorId).ThrowIfError();
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}