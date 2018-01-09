using System.Threading;

namespace Perpetuum.IDGenerators
{
    public class IntIDGenerator : IIDGenerator<int>
    {
        private int _id;

        public IntIDGenerator(int startID)
        {
            _id = startID;
        }

        public int GetNextID()
        {
            return Interlocked.Increment(ref _id);
        }
    }
}