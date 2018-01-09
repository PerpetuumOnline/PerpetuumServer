namespace Perpetuum.Log
{
    public interface ILogger { }

    public interface ILogger<in T> : ILogger where T:ILogEvent
    {
        void Log(T logEvent);
    }
}