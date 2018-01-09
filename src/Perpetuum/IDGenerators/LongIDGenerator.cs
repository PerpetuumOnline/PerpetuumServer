using System.Threading;

namespace Perpetuum.IDGenerators
{
    internal class LongIDGenerator : IIDGenerator<long>
    {
        private long _id;

        public long GetNextID()
        {
            return Interlocked.Increment(ref _id);
        }
    }
}