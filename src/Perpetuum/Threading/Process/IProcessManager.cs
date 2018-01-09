using System;
using System.Collections.Generic;

namespace Perpetuum.Threading.Process
{
    public interface IProcessManager
    {
        void Start();
        void Stop();
        void AddProcess(IProcess process);
        void RemoveProcess(IProcess process);

        IEnumerable<IProcess> Processes { get; }
    }

    public static class ProcessManagerExtensions
    {
        public static bool RemoveFirstProcess(this IProcessManager processManager, Func<IProcess, bool> predicate)
        {
            foreach (var process in processManager.Processes)
            {
                if (predicate(process))
                {
                    processManager.RemoveProcess(process);
                    return true;
                }
            }

            return false;
        }
    }

}