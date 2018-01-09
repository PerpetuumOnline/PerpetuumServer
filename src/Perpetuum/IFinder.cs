namespace Perpetuum
{
    public interface IFinder<T>
    {
        bool Find(out T result);
    }
}