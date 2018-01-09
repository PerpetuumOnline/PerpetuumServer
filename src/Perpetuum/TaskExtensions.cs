using System;
using System.Threading.Tasks;
using Perpetuum.Log;

namespace Perpetuum
{
    public static class TaskExtensions
    {
        public static Task LogExceptions(this Task task)
        {
            return task.ContinueWith(t =>
            {
                try
                {
                    t.Wait();
                }
                catch (Exception exception)
                {
                    var aex = exception as AggregateException;
                    if (aex != null)
                    {
                        aex.Handle(ex =>
                        {
                            Logger.Exception(ex);
                            return true;
                        });
                    }
                    else
                    {
                        Logger.Exception(exception);
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
