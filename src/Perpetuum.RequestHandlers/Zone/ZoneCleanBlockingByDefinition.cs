using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCleanBlockingByDefinition : IRequestHandler<IZoneRequest>
    {
        private readonly IEntityDefaultReader _defaultReader;

        public ZoneCleanBlockingByDefinition(IEntityDefaultReader defaultReader)
        {
            _defaultReader = defaultReader;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var definitions = request.Data.GetOrDefault<int[]>(k.definition);

            foreach (var definition in definitions)
            {
                var ed = _defaultReader.Get(definition);
                if (ed == EntityDefault.None)
                {
                    Logger.Error("definition not found:" + definition);
                    continue;
                }

                request.Zone.CleanBlockingByDefinition(ed);
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}