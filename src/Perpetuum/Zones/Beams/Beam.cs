using System;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.ExportedTypes;
using Perpetuum.IDGenerators;
using Perpetuum.Threading;

namespace Perpetuum.Zones.Beams
{
    public delegate void BeamExpiredCallback(Beam beam);

    /// <summary>
    /// Multifuctional visual effect
    /// Used when a module is activated on a target or some event triggers a visual effect
    /// </summary>
    public class Beam : Disposable
    {
        private static readonly IIDGenerator<long> _idGenerator = IDGenerator.CreateLongIDGenerator();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public Beam(BeamType type, TimeSpan duration)
        {
            Id = _idGenerator.GetNextID();
            Created = DateTime.Now;
            Type = type;
            Duration = duration;
        }

        public void Start()
        {
            Task.Delay(Duration, _tokenSource.Token).ContinueWith(t =>
                {
                    if (t.IsCanceled)
                        return;

                    Expired?.Invoke(this);
                });
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _tokenSource.Cancel();
            Expired = null;
        }

        public BeamExpiredCallback Expired { private get; set; }

        public long Id { get; private set; }
        public BeamType Type { get; private set; }
        public DateTime Created { get; private set; }
        public TimeSpan Duration { get; private set; }

        public byte Slot { get; internal set; }
        public long SourceEid { get; internal set; }
        public Position SourcePosition { get; internal set; }
        public long TargetEid { get; internal set; }
        public Position TargetPosition { get; internal set; }
        public BeamState State { get; internal set; }
        public double BulletTime { get; internal set; }
        public int Visibility { get; internal set; }

        public static BeamBuilder NewBuilder()
        {
            return new BeamBuilder();
        }
    }

}