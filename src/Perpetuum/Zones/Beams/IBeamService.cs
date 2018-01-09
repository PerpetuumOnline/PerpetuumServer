using System;
using System.Collections.Generic;

namespace Perpetuum.Zones.Beams
{
    public interface IBeamService : IObservable<Beam>
    {
        void Add(Beam beam);
        void Clear();

        IEnumerable<Beam> All { get; }
    }
}