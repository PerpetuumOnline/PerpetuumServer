namespace Perpetuum.IDGenerators
{
    public interface IIDGenerator<out T>
    {
        T GetNextID();
    }
}