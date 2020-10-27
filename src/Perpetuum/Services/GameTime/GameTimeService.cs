using Perpetuum.Reactive;
using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Services.Daytime
{
    public class GameTimeService : Process, IGameTimeService
    {
        private readonly Observable<GameTimeInfo> _observable;
        private GameTimeInfo _current;
        public GameTimeService()
        {
            _current = GameTimeInfo.FromCurrentTime();
            _observable = new AnonymousObservable<GameTimeInfo>(OnSubscribe);
        }

        private void RefreshCurrentDayTime()
        {
            _current = GameTimeInfo.FromCurrentTime();
        }

        public GameTimeInfo GetCurrentDayTime()
        {
            if (_current == null)
            {
                RefreshCurrentDayTime();
            }
            return _current;
        }

        private void OnSubscribe(IObserver<GameTimeInfo> observer)
        {
            observer.OnNext(GetCurrentDayTime());
        }

        public IDisposable Subscribe(IObserver<GameTimeInfo> observer)
        {
            return _observable.Subscribe(observer);
        }

        private void SendDayTimeNotification()
        {
            _observable.OnNext(GetCurrentDayTime());
        }

        public override void Update(TimeSpan time)
        {
            RefreshCurrentDayTime();
            SendDayTimeNotification();
        }
    }
}
