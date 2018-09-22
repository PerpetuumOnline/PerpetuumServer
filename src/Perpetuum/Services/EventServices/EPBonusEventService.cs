using Perpetuum.Threading.Process;
using Perpetuum.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Services.EventServices
{
    public class EPBonusEventService : Process, IDisposable
	{
		private readonly TimeSpan THREAD_TIMEOUT = TimeSpan.FromSeconds(1);
		private bool _disposedValue = false;

		private TimeSpan _duration;
        private TimeSpan _elapsed;
        private bool _eventStarted;
        private bool _endingEvent;
        private int _bonus;
        private ReaderWriterLockSlim _lock;

        public EPBonusEventService()
        {
			_lock = new ReaderWriterLockSlim();
			Init();
        }

        private void Init()
        {
			using (_lock.Write(THREAD_TIMEOUT))
			{
				_bonus = 0;
				_duration = TimeSpan.MaxValue;
				_elapsed = TimeSpan.Zero;
				_eventStarted = false;
				_endingEvent = false;
			}
        }

        public int GetBonus()
        {
			using (_lock.Read(THREAD_TIMEOUT))
				return _bonus;
        }

        public void SetEvent(int bonus, TimeSpan duration)
        {
			using (_lock.Write(THREAD_TIMEOUT))
			{
				_bonus = bonus;
				_elapsed = TimeSpan.Zero;
				_duration = duration;
				_endingEvent = false;
				_eventStarted = true;
			}
        }

        private void EndEvent()
        {
			using (_lock.Write(THREAD_TIMEOUT))
			{
				_bonus = 0;
				_elapsed = TimeSpan.Zero;
				_duration = TimeSpan.MaxValue;
				_eventStarted = false;
				_endingEvent = false;
			}
        }

        public override void Update(TimeSpan time)
        {
			using (_lock.Read(THREAD_TIMEOUT))
			{

				if (!_eventStarted)
					return;

				if (_endingEvent)
					return;
			}
			
			using (_lock.Write(THREAD_TIMEOUT))
			{
				_elapsed += time;
				if (_elapsed < _duration)
					return;
				_endingEvent = true;
			}

            Task.Run(() => EndEvent());
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Start()
        {
            Init();
            base.Start();
        }

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_lock?.Dispose();
					_lock = null; // This shouldn't be necessary but added for good practice
				}

				_disposedValue = true;
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}
