using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// Base class for all EventProcessors
    /// </summary>
    /// <typeparam name="EventMessage"></typeparam>
    public abstract class EventProcessor<EventMessage> : IObserver<EventMessage>
    {
        public abstract void OnNext(EventMessage value);

        void IObserver<EventMessage>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<EventMessage>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
