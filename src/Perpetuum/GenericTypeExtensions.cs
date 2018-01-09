using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Perpetuum
{
    public static class GenericTypeExtensions
    {
        /// <summary>
        /// Maps a struct array to a byte array
        /// </summary>
        public static byte[] ToByteArray<T>(this T[] array) where T : struct
        {
            Debug.Assert(array != null);

            var result = new byte[array.Length * Marshal.SizeOf(typeof(T))];

            if (typeof(T).IsPrimitive)
            {
                Buffer.BlockCopy(array, 0, result, 0, result.Length);
            }
            else
            {
                var sHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                Marshal.Copy(sHandle.AddrOfPinnedObject(), result, 0, result.Length);
                sHandle.Free();
            }

            return result;
        }

        public static byte[] ToByteArray<T>(this T source) where T : struct
        {
            var size = Marshal.SizeOf(source);
            var ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(source, ptr, true);
                var array = new byte[size];
                Marshal.Copy(ptr, array, 0, size);
                return array;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        
        public static T Clone<T>(this T source)
        {
            if (Equals(source, default(T)))
            {
                return default(T);
            }

            Debug.Assert(typeof(T).IsSerializable, "EZ NEM SERIALIZALHATO: " + typeof(T));

            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, source);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)(bf.Deserialize(ms));
            }
        }

    }
}
