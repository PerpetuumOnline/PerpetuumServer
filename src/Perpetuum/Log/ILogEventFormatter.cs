
namespace Perpetuum.Log
{
    public interface ILogEventFormatter<in T, out TOut> where T:ILogEvent
    {
        TOut Format(T logEvent);
    }
}