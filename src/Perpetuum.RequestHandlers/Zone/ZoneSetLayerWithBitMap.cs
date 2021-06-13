using System.Drawing;
using Perpetuum.Host.Requests;
using Perpetuum.IO;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSetLayerWithBitMap : IRequestHandler<IZoneRequest>
    {
        private readonly IFileSystem _fileSystem;
        public ZoneSetLayerWithBitMap(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var zone = request.Zone;
            zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            var fileName = request.Data.GetOrDefault<string>(k.file);
            var flagValue = request.Data.GetOrDefault<int>(k.flags);
            var controlFlag = EnumHelper.GetEnum<TerrainControlFlags>(flagValue);
            var path = _fileSystem.CreatePath("bitmaps", zone.CreateTerrainDataFilename(fileName, "png"));
            var img = Image.FromFile(path);
            using (Bitmap bmp = new Bitmap(img))
            {
                zone.Terrain.Controls.UpdateAll((x, y, c) =>
                {
                    if (bmp.GetPixel(x, y).A == 0)
                    {
                        return c;
                    }
                    c.SetFlags(controlFlag, true);
                    return c;
                });
            }
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}
