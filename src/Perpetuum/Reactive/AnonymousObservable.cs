using System;

namespace Perpetuum.Reactive
{
    public class AnonymousObservable<T> : Observable<T>
    {
        private readonly Action<IObserver<T>> _onSubscribe;

        public AnonymousObservable(Action<IObserver<T>> onSubscribe)
        {
            _onSubscribe = onSubscribe;
        }

        protected override void OnSubscribe(IObserver<T> observer)
        {
            _onSubscribe(observer);
            base.OnSubscribe(observer);
        }
    }
}