using Perpetuum.Threading.Process;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.EventServices.EventProcessors;
using Perpetuum.Timers;

namespace Perpetuum.Services.EventServices
{
    /// <summary>
    /// Listener Process that processes queue of EventMessages and notifies observers
    /// </summary>
    public class EventListenerService : Process
    {
        private readonly object _lock = new object();
        private readonly IDictionary<EventType, IList<IEventProcessor>> _observers;
        private readonly ConcurrentQueue<IEventMessage> _queue;

        public EventListenerService()
        {
            _observers = new Dictionary<EventType, IList<IEventProcessor>>();
            _queue = new ConcurrentQueue<IEventMessage>();
        }

        /// <summary>
        /// Send a message where the type of EventMessage determines which listener is notified
        /// </summary>
        /// <param name="message">EventMessage of the type</param>
        public void PublishMessage(IEventMessage message)
        {
            _queue.Enqueue(message);
        }

        public void NotifyListeners(IEventMessage message)
        {
            lock (_lock)
            {
                if(_observers.TryGetValue(message.Type, out var list))
                {
                    foreach (var obs in list)
                    {
                        obs.OnNext(message);
                    }
                }
            }
        }

        /// <summary>
        /// Listeners are subscribed and instantiated in the Bootstrapper
        /// </summary>
        /// <param name="observer">Listener</param>
        public void AttachListener(IEventProcessor observer)
        {
            lock (_lock)
            {
                if (!_observers.ContainsKey(observer.Type))
                {
                    _observers.Add(observer.Type, new List<IEventProcessor>() { observer });
                }
                else
                {
                    _observers[observer.Type].Add(observer);
                }
            }
        }

        public override void Update(TimeSpan time)
        {
            var timer = new TimeKeeper(TimeSpan.FromSeconds(1));
            while (!_queue.IsEmpty && !timer.Expired)
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
