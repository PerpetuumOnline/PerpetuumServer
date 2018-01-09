using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Reactive;

namespace Perpetuum.Zones.Beams
{
    public class BeamService : IBeamService
    {
        private readonly ConcurrentDictionary<long,Beam> _beams = new ConcurrentDictionary<long, Beam>();
        private readonly Observable<Beam> _observable;

        public BeamService()
        {
            _observable = new AnonymousObservable<Beam>(OnSubscribe);
        }

        private void OnSubscribe(IObserver<Beam> observer)
        {
            foreach (var kvp in _beams)
            {
                observer.OnNext(kvp.Value);
            }
        }

        public void Add(Beam beam)
        {
            if ( beam.Type == BeamType.undefined )
                return;

            _beams[beam.Id] = beam;

            beam.Expired = b => Remove(beam);
            beam.Start();

            _observable.OnNext(beam);
        }

        public void Clear()
        {
            foreach (var beam in _beams.Values)
            {
                Remove(beam);
            }
        }

        public IEnumerable<Beam> All { get { return _beams.Select(kvp => kvp.Value); } }

        private bool Remove(Beam beam)
        {
            try
            {
                return _beams.Remove(beam.Id);
            }
            finally
            {
                beam.Dispose();
            }
        }

        public IDisposable Subscribe(IObserver<Beam> observer)
        {
            return _observable.Subscribe(observer);
        }
    }
}