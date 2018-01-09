using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawBlockingByDefinition : IRequestHandler<IZoneRequest>
    {
        private readonly IEntityDefaultReader _defaultReader;

        public ZoneDrawBlockingByDefinition(IEntityDefaultReader defaultReader)
        {
            _defaultReader = defaultReader;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var definitionz = request.Data.GetOrDefault<int[]>(k.definition);

            foreach (var definition in definitionz)
            {
                var ed = _defaultReader.Get(definition);
                if (ed == EntityDefault.None)
                {
                    Logger.Error("definition not found:" + definition);
                    continue;
                }

                request.Zone.DrawBlockingByDefinition(ed);
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}