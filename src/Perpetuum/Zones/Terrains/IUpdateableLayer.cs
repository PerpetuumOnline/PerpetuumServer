using System.IO;

namespace Perpetuum.Zones.Terrains
{
    public interface IUpdateableLayer
    {
        int SizeInBytes { get; }
        void CopyFromStreamToArea(Stream stream, Area area);
        byte[] CopyAreaToByteArray(Area area);
    }
}