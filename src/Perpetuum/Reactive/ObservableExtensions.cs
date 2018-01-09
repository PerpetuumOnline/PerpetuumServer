using System;

namespace Perpetuum.Reactive
{
    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext)
        {
            var o = new AnonymousObserver<T>(onNext,Stubs<Exception>.None,Stubs.None);
            return observable.Subscribe(o);
        }
    }
}