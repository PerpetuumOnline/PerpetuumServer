using System;

namespace Perpetuum.Threading.Process
{
    public interface IProcess
    {
        void Start();
        void Stop();
        void Update(TimeSpan time);
    }
}