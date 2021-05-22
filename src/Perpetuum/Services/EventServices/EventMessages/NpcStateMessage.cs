using System;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public enum NpcState
    {
        Alive,
        Dead
    }

    /// <summary>
    /// A Simple EventMessage stub with string content, mostly to demonstrate usage
    /// </summary>
    public class NpcStateMessage : IEventMessage
    {
        public EventType Type => EventType.NpcState;
        public int FlockId { get; }
        public NpcState State { get; }
        public DateTime TimeStamp { get; }
        public NpcStateMessage(int flockId, NpcState state, DateTime time)
        {
            FlockId = flockId;
            State = state;
            TimeStamp = time;
        }
    }
}
