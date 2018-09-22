using System;
using System.Threading;

namespace Perpetuum.Threading
{
	public static class ThreadLockExtensions
	{
		private sealed class ReadLock : IDisposable
		{
			private ReaderWriterLockSlim syncLock;
			private bool disposed = false;

			public ReadLock(ReaderWriterLockSlim syncLock, TimeSpan timeout)
			{
				this.syncLock = syncLock ?? throw new ArgumentNullException("syncLock", "Parameter cannot be null");

				if (!this.syncLock.TryEnterReadLock(timeout))
				{
					this.syncLock = null;
					throw new TimeoutException("Timed-out waiting for read lock");
				}
			}

			public void Dispose()
			{
				if (!disposed)
				{
					syncLock?.ExitReadLock();
					syncLock = null;

					disposed = true;
				}
			}
		}

		private sealed class WriteLock : IDisposable
		{
			private ReaderWriterLockSlim syncLock;
			private bool disposed = false;

			public WriteLock(ReaderWriterLockSlim syncLock, TimeSpan timeout)
			{
				this.syncLock = syncLock ?? throw new ArgumentNullException("syncLock", "Parameter cannot be null");

				if (!this.syncLock.TryEnterReadLock(timeout))
				{
					this.syncLock = null;
					throw new TimeoutException("Timed-out waiting for write lock");
				}
			}
			public void Dispose()
			{
				if (!disposed)
				{
					syncLock?.ExitReadLock();
					syncLock = null;

					disposed = true;
				}
			}
		}

		public static IDisposable Read(this ReaderWriterLockSlim obj, TimeSpan timeout)
		{
			return new ReadLock(obj, timeout);
		}

		public static IDisposable Write(this ReaderWriterLockSlim obj, TimeSpan timeout)
		{
			return new WriteLock(obj, timeout);
		}
	}
}
