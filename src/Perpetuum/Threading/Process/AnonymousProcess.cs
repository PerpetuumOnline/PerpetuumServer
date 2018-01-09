using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Threading.Process
{
    public class AnonymousProcess : IProcess
    {
        private readonly Action<TimeSpan> _updater;

        public AnonymousProcess(Action<TimeSpan> updater)
        {
            _updater = updater;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            _updater(time);
        }
    }

    public class CompositeProcess : IProcess
    {
        private readonly List<IProcess> _processes = new List<IProcess>();

        public CompositeProcess()
        {
            
        }

        public CompositeProcess(IEnumerable<IProcess> processes)
        {
            _processes = processes.ToList();
        }

        public void AddProcess(IProcess process)
        {
            _processes.Add(process);
        }

        public void Start()
        {
            foreach (var process in _processes)
            {
                process.Start();
            }
        }

        public void Stop()
        {
            foreach (var process in _processes)
            {
                process.Stop();
            }
        }

        public void Update(TimeSpan time)
        {
            foreach (var process in _processes)
            {
                process.Update(time);
            }
        }
    }
}