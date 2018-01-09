using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZonePlaceWall : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var player = request.Zone.GetPlayerOrThrow(character);

            var terrainLock = player.GetPrimaryLock() as TerrainLock;
            if ( terrainLock == null )
                return;

            var direction = terrainLock.Location - player.CurrentPosition;

            var distance = direction.Length;

            direction.Normalize();

            using (new TerrainUpdateMonitor(request.Zone))
            {
                for (int i = 0; i < distance; i++)
                {
                    var p = player.CurrentPosition + (direction * i);

                    request.Zone.Terrain.Plants.UpdateValue(p,pi =>
                    {
                        pi.SetPlant(1,PlantType.Wall);
                        pi.health = 255;
                        pi.state = 11;
                        return pi;
                    });

                    request.Zone.Terrain.Blocks.UpdateValue(p,bi =>
                    {
                        bi.Height = 14;
                        bi.Plant = true;
                        return bi;
                    });

                    request.Zone.CreateAlignedDebugBeam(BeamType.orange_10sec,p);
                }
            }
        }
    }
}