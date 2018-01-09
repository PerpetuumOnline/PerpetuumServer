using System;

namespace Perpetuum.Reactive
{
    public class AnonymousObserver<T> : Observer<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public AnonymousObserver(Action<T> onNext,Action<Exception> onError,Action onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public override void OnNext(T value)
        {
            _onNext(value);
        }

        public override void OnError(Exception error)
        {
            _onError(error);
        }

        public override void OnCompleted()
        {
            _onCompleted();
        }
    }
}