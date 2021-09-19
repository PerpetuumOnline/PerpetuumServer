using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCreateTerraformLimit : IRequestHandler<IZoneRequest>
    {
        private void Clear(IZoneRequest request)
        {
            var zone = request.Zone;
            var area = Area.FromRectangle(0, 0, zone.Size.Width, zone.Size.Height);
            var workAreas = area.Slice(32);

            Parallel.ForEach(workAreas, (workArea) =>
            {
                var controlArea = zone.Terrain.Controls.GetArea(workArea);
                for(int i=0; i< controlArea.Length; i++)
                {
                    controlArea[i].TerraformProtected = false;
                    controlArea[i].PBSTerraformProtected = false;
                }
                zone.Terrain.Controls.SetArea(workArea, controlArea);
            });
        }

        private void SetRadiusOnTeleports(IZoneRequest request, int radius)
        {
            var zone = request.Zone;
            var maxX = zone.Terrain.Controls.Width-1;
            var maxY = zone.Terrain.Controls.Height-1;
            var teleports = request.Zone.GetTeleportColumns();
            foreach (var tele in teleports)
            {
                var center = tele.CurrentPosition.ToPoint();
                var x0 = (center.X - radius).Clamp(0, maxX);
                var x1 = (center.X + radius).Clamp(0, maxX);
                var y0 = (center.Y - radius).Clamp(0, maxY);
                var y1 = (center.Y + radius).Clamp(0, maxY);

                var area = Area.FromRectangle(x0, y0, x1, y1);
                var workAreas = area.Slice(32);
                Parallel.ForEach(workAreas, (workArea) =>
                {
                    foreach (var p in workArea.GetPositions())
                    {
                        if(!p.intX.IsInRange(0, maxY) || !p.intY.IsInRange(0, maxY))
                        {
                            continue;
                        }
                        else if (center.Distance(p) < radius)
                        {
                            var c = zone.Terrain.Controls[p.intX, p.intY];
                            c.TerraformProtected = true;
                            c.PBSTerraformProtected = true;
                            zone.Terrain.Controls[p.intX, p.intY] = c;
                        }
                    }
                });
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