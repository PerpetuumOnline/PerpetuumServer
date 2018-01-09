using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneFixPBS : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            //letorojuk a terraform protection bitet
            request.Zone.Terrain.Controls.UpdateAll((x, y, info) =>
            {
                info.PBSTerraformProtected = false;
                return info;
            });

            foreach (var unit in request.Zone.Units)
            {
                if (unit is TeleportColumn)
                    continue;

                var position = unit.CurrentPosition.Center;

                if (unit.TryGetConstructionRadius(out int constructionRadius))
                    LayerHelper.SetTerrafomProtectionCircle(request.Zone, position, constructionRadius);

                if (unit is IPBSObject)
                    LayerHelper.SetConcreteCircle(request.Zone, position, constructionRadius);
            }
        }
    }
}