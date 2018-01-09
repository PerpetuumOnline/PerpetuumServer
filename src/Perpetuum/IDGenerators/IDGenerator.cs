namespace Perpetuum.IDGenerators
{
    public static class IDGenerator
    {
        public static IIDGenerator<int> CreateIntIDGenerator(int startID = 0)
        {
            return new IntIDGenerator(startID);
        }

        public static IIDGenerator<long> CreateLongIDGenerator()
        {
            return new LongIDGenerator();
        }
    }
}