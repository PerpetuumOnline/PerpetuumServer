using System;
using System.IO;
using System.IO.Compression;

namespace Perpetuum
{
    /// <summary>
    /// GZip compressor's interface. Standard GZip implementation
    /// </summary>
    public static class GZip
    {
        public static byte[] Compress(byte[] bytesToCompress)
        {
            return Compress(bytesToCompress, 0, bytesToCompress.Length);
        }

        public static unsafe byte[] Compress(byte[] bytesToCompress,int index,int length)
        {
            using (var ms = new MemoryStream())
            {
                var gzStream = new GZipStream(ms, CompressionMode.Compress, false);
                gzStream.Write(bytesToCompress, index,length);
                gzStream.Close();

                var tmpArray = ms.GetBuffer();

                var outBytes = new byte[tmpArray.Length + 4];

                fixed (byte* pOutBytes = outBytes)
                {
                    *(int*)pOutBytes = length;
                }

                Array.Copy(tmpArray, 0, outBytes, 4, tmpArray.Length);
                return outBytes;
            }
        }

        public static byte[] Decompress(byte[] bytesToDecompress,int seek = 4)
        {
            using (var originalStream = (Stream) new MemoryStream(bytesToDecompress))
            {
                originalStream.Seek(seek, SeekOrigin.Begin);

                using (var gZipStream = new GZipStream(originalStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        gZipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }
    }
}