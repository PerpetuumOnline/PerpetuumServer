using Perpetuum.Data;
using Perpetuum.Log;

namespace Perpetuum.Common.Loggers
{
    public abstract class DbLogger<T> : ILogger<T> where T:ILogEvent
    {
        public void Log(T logEvent)
        {
            var query = Db.Query();
            BuildCommand(logEvent, query);
            var res = query.ExecuteNonQuery();
            if (res == 0)
                throw new PerpetuumException(ErrorCodes.SQLInsertError);
        }

        protected abstract void BuildCommand(T logEvent, DbQuery builder);
    }
}