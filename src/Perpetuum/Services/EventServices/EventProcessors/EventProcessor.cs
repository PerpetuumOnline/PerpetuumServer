using Perpetuum.Services.EventServices.EventMessages;
using System;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    public interface IEventProcessor : IObserver<EventMessage>
    {
        void HandleMessage(EventMessage value);
    }

    public abstract class EventProcessor : IEventProcessor
    {
        public abstract void HandleMessage(EventMessage value);

        public void OnNext(EventMessage value)
        {
            HandleMessage(value);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
