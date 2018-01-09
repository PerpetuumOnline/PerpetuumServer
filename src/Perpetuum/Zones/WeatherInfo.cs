using System;
using Perpetuum.Timers;

namespace Perpetuum.Zones
{
    public class WeatherInfo
    {
        private readonly int _current;
        public int Next { get; private set; }

        private readonly TimeTracker _timer;

        public WeatherInfo(int current, int next,TimeSpan duration)
        {
            _current = current;
            Next = next;
            _timer = new TimeTracker(duration);
        }

        public bool Update(TimeSpan elapsed)
        {
            _timer.Update(elapsed);
            return _timer.Expired;
        }

        public Packet CreateUpdatePacket()
        {
            var packet = new Packet(ZoneCommand.Weather);
            packet.AppendByte(0); // ez lesz majd a 
            packet.AppendByte((byte)_current);
            packet.AppendByte((byte)Next);
            packet.AppendLong((long)_timer.Elapsed.TotalMilliseconds);
            packet.AppendLong((long)_timer.Duration.TotalMilliseconds);
            return packet;
        }
    }
}