using Perpetuum.IDGenerators;

namespace Perpetuum.EntityFramework
{
    public abstract class EntityIDGenerator : IIDGenerator<long>
    {
        public static EntityIDGenerator Fix(long eid)
        {
            return new FixIDGenerator(eid);
        }

        public static EntityIDGenerator Random { get; } = new RandomIDGenerator();

        private class RandomIDGenerator : EntityIDGenerator
        {
            public override long GetNextID()
            {
                // 62. bit mindig 1!
                return FastRandom.NextLong() | 0x4000000000000000;
            }
        }

        private class FixIDGenerator : EntityIDGenerator
        {
            private readonly long _eid;

            public FixIDGenerator(long eid)
            {
                _eid = eid;
            }

            public override long GetNextID()
            {
                return _eid;
            }
        }

        public abstract long GetNextID();
    }
}