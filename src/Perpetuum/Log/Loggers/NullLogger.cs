namespace Perpetuum.Log.Loggers
{
    public class NullLogger<T> : ILogger<T> where T:ILogEvent
    {
        public void Log(T logEvent)
        {
        }
    }
}