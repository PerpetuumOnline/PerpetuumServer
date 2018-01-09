using System;
using System.Threading.Tasks;
using Perpetuum.EntityFramework;
using Perpetuum.Timers;
using Perpetuum.Units;

namespace Perpetuum.Zones.PBS
{
    public class PBSObjectSaver<T> where T : Unit, IPBSObject
    {
        private readonly IEntityRepository _entityRepository;
        private readonly IntervalTimer _timer;

        public PBSObjectSaver(IEntityRepository entityRepository,TimeSpan saveInterval)
        {
            _entityRepository = entityRepository;
            _timer = new IntervalTimer(saveInterval);
        }

        private bool _running;

        public void Update(T pbsObject,TimeSpan time)
        {
            if (_running)
                return;

            _timer.Update(time);
            if ( !_timer.Passed )
                return;
            _timer.Reset();

            _running = true;
            Task.Run(() => _entityRepository.Update(pbsObject))
                .ContinueWith(t => _running = false)
                .LogExceptions();
        }
    }   
}