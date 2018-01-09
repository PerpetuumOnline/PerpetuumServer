using System;
using System.Collections.Immutable;
using Perpetuum.Threading;

namespace Perpetuum.Reactive
{
    public class Observable<T> : IObservable<T>
    {
        private ImmutableHashSet<IObserver<T>> _observers = ImmutableHashSet<IObserver<T>>.Empty;

        public void OnNext(T value)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(value);
            }
        }

        public void OnError(Exception exception)
        {
            foreach (var observer in _observers)
            {
                observer.OnError(exception);
            }
        }

        public void OnCompleted()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (_observers.Contains(observer))
                return Disposable.Empty;

            // ReSharper disable once ImplicitlyCapturedClosure
            ImmutableInterlocked.Update(ref _observers, o => o.Add(observer));

            OnSubscribe(observer);

            return Disposable.Create(() => { Unsubscribe(observer); });
        }

        public void Unsubscribe(IObserver<T> observer)
        {
            ImmutableInterlocked.Update(ref _observers, o => o.Remove(observer));
        }

        protected virtual void OnSubscribe(IObserver<T> observer)
        {
            
        }

        public static Observable<T> Create(Action<IObserver<T>> onSubscribe)
        {
            return new AnonymousObservable<T>(onSubscribe);
        }
    }
}