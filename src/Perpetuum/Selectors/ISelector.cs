namespace Perpetuum.Selectors
{
    public interface ISelector<out T>
    {
        T GetNext();
    }

}
