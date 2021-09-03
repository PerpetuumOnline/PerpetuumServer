using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneClearLayer : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var layerName = request.Data.GetOrDefault<string>(k.layerName);

            switch (layerName)
            {
                case k.groundType:
                    request.Zone.Terrain.Plants.UpdateAll((x, y, pi) =>
                    {
                        pi.ClearGroundType();
                        return pi;
                    });
                    break;
                case k.plants:
                    request.Zone.Terrain.Plants.UpdateAll((x, y, pi) =>
                    {
                        pi.Clear();

                        // Update blocking footprint of plants for this tile
                        request.Zone.Terrain.Blocks.UpdateValue(x, y, bi =>
                        {
                            // only reset the height if there is a plant here.
                            // otherwise we reset blocking heights for decor, etc!
                            bi.Height = bi.Plant ? 0 : bi.Height;
                            bi.Plant = false;
                            return bi;
                        });
                        return pi;
                    });
                    break;
                case k.control:
                    request.Zone.Terrain.Controls.UpdateAll((x, y, ci) => new TerrainControlInfo());
                    break;

                case k.blocks:
                    request.Zone.Terrain.Blocks.UpdateAll((x, y, bi) => new BlockingInfo());
                    break;
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}