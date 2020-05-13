using Perpetuum.Threading.Process;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;

namespace Perpetuum.Services.EventServices
{
    /// <summary>
    /// Listener Process that processes queue of EventMessages and notifies observers
    /// </summary>
    public class EventListenerService : Process
    {
        private readonly IList<EventProcessor<EventMessage>> _observers;
        private readonly ConcurrentQueue<EventMessage> _queue;

        public EventListenerService()
        {
            _observers = new List<EventProcessor<EventMessage>>();
            _queue = new ConcurrentQueue<EventMessage>();
        }

        /// <summary>
        /// Send a message where the type of EventMessage determines which listener is notified
        /// </summary>
        /// <param name="message">EventMessage of the type</param>
        public void PublishMessage(EventMessage message)
        {
            _queue.Enqueue(message);
        }

        public void NotifyListeners(EventMessage message)
        {
            foreach (var obs in _observers)
            {
                obs.OnNext(message);
            }
        }

        /// <summary>
        /// Listeners are subscribed and instantiated in the Bootstrapper
        /// </summary>
        /// <param name="observer">Listener</param>
        public void AttachListener(EventProcessor<EventMessage> observer)
        {
            _observers.Add(observer);
        }

        public override void Update(TimeSpan time)
        {
            while (!_queue.IsEmpty)
            {
                if (_queue.TryDequeue(out var message))
                {
                    NotifyListeners(message);
                }
            }
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Start()
        {
            base.Start();
        }
    }
}
