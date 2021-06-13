using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;
using System.Drawing;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCreateTerraformLimit : IRequestHandler<IZoneRequest>
    {
        private void Clear(IZoneRequest request)
        {
            request.Zone.Terrain.Controls.UpdateAll((x, y, c) =>
            {
                c.TerraformProtected = false;
                return c;
            });
        }

        private void SetRadiusOnTeleports(IZoneRequest request, int radius)
        {
            var zone = request.Zone;
            var teleports = request.Zone.GetTeleportColumns();
            foreach (var tele in teleports)
            {
                var center = tele.CurrentPosition.ToPoint();
                var x0 = (center.X - radius).Clamp(0, zone.Terrain.Controls.Width);
                var x1 = (center.X + radius).Clamp(0, zone.Terrain.Controls.Width);
                var y0 = (center.Y - radius).Clamp(0, zone.Terrain.Controls.Height);
                var y1 = (center.Y + radius).Clamp(0, zone.Terrain.Controls.Height);
                for (var y = y0; y < y1; y++)
                {
                    for (var x = x0; x < x1; x++)
                    {
                        var p = new Point(x, y);
                        if (center.Distance(p) < radius)
                        {
                            zone.Terrain.Controls.UpdateValue(x, y, (c) =>
                            {
                                c.TerraformProtected = true;
                                return c;
                            });
                        }
                    }
                }
            }
        }

        public void HandleRequest(IZoneRequest request)
        {
            request.Zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var radius = request.Data.GetOrDefault<int>(k.distance);
            var mode = request.Data.GetOrDefault<string>(k.mode);
            radius = radius.Clamp(0, 500);

            if (mode == "clear")
            {
                Clear(request);
            }
            else if (mode == "teleports")
            {
                SetRadiusOnTeleports(request, radius);
            }
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}