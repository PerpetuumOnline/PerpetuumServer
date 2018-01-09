using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSampleDecorEnvironment : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var decorId = request.Data.GetOrDefault<int>(k.ID);
                var range = request.Data.GetOrDefault<int>(k.range);

                range = range * 2;

                request.Zone.DecorHandler.SampleDecorEnvironment(decorId, range, out Dictionary<string, object> result).ThrowIfError();

                var lista = request.Zone.Environment.ListEnvironmentDescriptions();
                result.Add(k.definition, lista);
                Message.Builder.SetCommand(Commands.ZoneEnvironmentDescriptionList).WithData(result).ToClient(request.Session).Send();
                
                scope.Complete();
            }
        }
    }
}