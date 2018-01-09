
namespace Perpetuum
{
    public static class ObjectHelper
    {
        /// <summary>
        /// Swaps two objects
        /// </summary>
        public static void Swap<T>(ref T src, ref T dest)
        {
            var t = src;
            src = dest;
            dest = t;
        }

        public static int CombineHashCodes(int h1,int h2)
        {
            unchecked
            {
                return (((h1 << 5) + h1) ^ h2);             
            }
        }
    }
}