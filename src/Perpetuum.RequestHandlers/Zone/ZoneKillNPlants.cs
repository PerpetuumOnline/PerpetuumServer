using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneKillNPlants : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var plantIndex = request.Data.GetOrDefault<int>(k.ID);
            var everyNth = request.Data.GetOrDefault<int>(k.value);

            var counter = 0;

            request.Zone.Terrain.Plants.UpdateAll((x, y, pi) =>
            {
                if ((int) pi.type == plantIndex)
                {
                    //this is the plant we are looking for

                    if (counter++%everyNth == 0)
                    {
                        //kill it
                        pi.Clear();

                        request.Zone.Terrain.Blocks.UpdateValue(x, y, bi =>
                        {
                            // only reset the height if there is a plant here.
                            // otherwise we reset blocking heights for decor, etc!
                            bi.Height = bi.Plant ? 0 : bi.Height;
                            bi.Plant = false;
                            return bi;
                        });
                    }
                }

                return pi;
            });

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}