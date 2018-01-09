using System;

namespace Perpetuum.Reactive
{
    public class Observer<T> : IObserver<T>, IDisposable
    {
        private IDisposable _cancellation;

        public void Subscribe(IObservable<T> observable)
        {
            _cancellation = observable.Subscribe(this);
        }

        public virtual void OnNext(T value)
        {
        }

        public virtual void OnError(Exception error)
        {
        }

        public virtual void OnCompleted()
        {
        }

        public void Dispose()
        {
            _cancellation?.Dispose();
            OnDispose();
        }

        protected virtual void OnDispose()
        {

        }

        public static Observer<T> Create(Action<T> onNext)
        {
            return new AnonymousObserver<T>(onNext,Stubs<Exception>.None,Stubs.None);
        }

        public static Observer<T> Create(Action<T> onNext,Action<Exception> onError,Action onCompleted)
        {
            return new AnonymousObserver<T>(onNext,onError,onCompleted);
        }
    }
}