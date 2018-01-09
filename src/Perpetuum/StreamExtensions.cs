using System.IO;

namespace Perpetuum
{
    public static class StreamExtensions
    {
        public static unsafe void CopyToPointer(this Stream stream,byte *dest, int offset, int count)
        {
            var p = dest + offset;

            var br = new BinaryReader(stream);

            if (count >= 16)
            {
                do
                {
                    ((long*)p)[0] = br.ReadInt64();
                    ((long*)p)[1] = br.ReadInt64();
                    p += 16;
                } while ((count -= 16) >= 16);
            }

            if (count <= 0)
                return;

            if ((count & 8) != 0)
            {
                ((long*)p)[0] = br.ReadInt64();
                p += 8;
            }

            if ((count & 4) != 0)
            {
                ((int*)p)[0] = br.ReadInt32();
                p += 4;
            }

            if ((count & 2) != 0)
            {
                ((ushort*)p)[0] = br.ReadUInt16();
                p += 2;
            }

            if ((count & 1) != 0)
            {
                p[0] = br.ReadByte();
            }
        }
    }

}
