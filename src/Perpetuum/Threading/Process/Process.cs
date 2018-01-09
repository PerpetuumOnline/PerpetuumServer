using System;

namespace Perpetuum.Threading.Process
{
    public abstract class Process : IProcess
    {
        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }

        public abstract void Update(TimeSpan time);

        public static IProcess Create(Action<TimeSpan> onUpdate)
        {
            return new AnonymousProcess(onUpdate);
        }
    }
}