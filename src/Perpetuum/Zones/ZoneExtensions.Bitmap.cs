using System.Drawing;
using System.Drawing.Imaging;
using Perpetuum.IO;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones
{
    public class SaveBitmapHelper
    {
        private readonly IFileSystem _fileSystem;

        public SaveBitmapHelper(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }


        public void SaveBitmap(IZone zone,Bitmap bitmap,string name)
        {
            var fn = _fileSystem.CreatePath("bitmaps",zone.CreateTerrainDataFilename(name,"png"));
            bitmap.Save(fn,ImageFormat.Png);
        }

    }


    public static partial class ZoneExtensions
    {
        public static Bitmap CreatePassableBitmap(this IZone zone, Color passableTileColor, Color islandTileColor = default(Color))
        {
            var skipIsland = islandTileColor.Equals(default(Color));

            var b = zone.CreateBitmap();
            b.WithGraphics(g => g.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 0)), 0, 0, zone.Size.Width - 1, zone.Size.Height - 1));
            
            return b.ForEach((bmp, x, y) =>
            {
                if (zone.Terrain.Blocks[x, y].Island)
                {
                    if (skipIsland) return; //island pixels will be black
                    bmp.SetPixel(x,y,islandTileColor); //OR optionally the supported color
                    return;
                }
                    
                if (!zone.Terrain.IsPassable(x,y))
                    return;

                bmp.SetPixel(x, y, passableTileColor);
            });
        }
        
        public static Bitmap CreateBitmap(this IZone zone)
        {
            var size = zone.Size;
            return new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
        }
    }


}
