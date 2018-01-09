using System;
using System.Collections.Generic;

namespace Perpetuum.Threading.Process
{
    public static class ProcessExtensions
    {
        public static IProcess AsTimed(this IProcess process,TimeSpan interval)
        {
            return new TimedProcess(process,interval);
        }
        
        public static IProcess ToAsync(this IProcess process)
        {
            return new AsyncProcess(process);
        }

        public static IProcess ToCompositeProcess(this IEnumerable<IProcess> processes)
        {
            return new CompositeProcess(processes);
        }

        public static bool Is<T>(this IProcess process) where T : class, IProcess
        {
            var t = process.As<T>();
            return t != null;
        }

        [CanBeNull]
        public static T As<T>(this IProcess process) where T : class, IProcess
        {
            while (true)
            {
                if (process is T t)
                    return t;

                if (process is ProcessDecorator decorator)
                {
                    process = decorator.InnerProcess;
                    continue;
                }

                return null;
            }
        }
    }
}