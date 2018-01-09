namespace Perpetuum.Builders
{
    public interface IBuilder<out T>
    {
        T Build();
    }
}