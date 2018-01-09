using System;
using System.Drawing;
using Perpetuum.Builders;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;

namespace Perpetuum.Zones.Beams
{
    public interface IBeamBuilder : IBuilder<Beam>
    {
        
    }

    public class BeamBuilder : IBeamBuilder
    {
        private const int BEAM_VISIBILITY = 100; 

        internal BeamBuilder()
        {
            _visibility = BEAM_VISIBILITY;
            _state = BeamState.AlignToTerrain;
        }

        public BeamType Type { get; private set; }

        public BeamBuilder WithType(BeamType type)
        {
            Type = type;
            return this;
        }

        private byte _slot;
        public BeamBuilder WithSlot(int slot)
        {
            _slot = (byte)slot;
            return this;
        }

        private long _sourceEid;
        public BeamBuilder WithSource(Unit unit)
        {
            if (unit == null)
                return this;

            _sourceEid = unit.Eid;
            return WithSourcePosition(unit.CurrentPosition);
        }

        private Position _sourcePosition;
        public BeamBuilder WithSourcePosition(Position position)
        {
            _sourcePosition = position;
            return this;
        }

        private long _targetEid;
        public BeamBuilder WithTarget(Unit target)
        {
            if (target == null)
                return this;

            _targetEid = target.Eid;
            return WithTargetPosition(target.CurrentPosition);
        }

        private Position _targetPosition;
        public BeamBuilder WithTargetPosition(Position position)
        {
            _targetPosition = position;
            return this;
        }

        private int _visibility;
        public BeamBuilder WithVisibility(int visibility)
        {
            if (visibility < BEAM_VISIBILITY)
                visibility = BEAM_VISIBILITY;

            _visibility = visibility;
            return this;
        }

        private BeamState _state;
        public BeamBuilder WithState(BeamState beamState)
        {
            _state = beamState;
            return this;
        }

        private double _bulletTime;
        public BeamBuilder WithBulletTime(double time)
        {
            _bulletTime = time;
            return this;
        }

        public BeamBuilder WithDuration(int duration)
        {
            return WithDuration(TimeSpan.FromMilliseconds(duration));
        }

        private TimeSpan _duration;
        public BeamBuilder WithDuration(TimeSpan duration)
        {
            _duration = duration;
            return this;
        }

        public BeamBuilder WithPosition(Point position)
        {
            return WithSourcePosition(position.ToPosition()).WithTargetPosition(position.ToPosition());
       }

        public BeamBuilder WithPosition(Position position)
        {
            return WithSourcePosition(position).WithTargetPosition(position);
        }

        public Beam Build()
        {
            return new Beam(Type,_duration)
                {
                    Slot = _slot, 
                    SourceEid = _sourceEid, 
                    SourcePosition = _sourcePosition, 
                    TargetEid = _targetEid, 
                    TargetPosition = _targetPosition, 
                    Visibility = _visibility, 
                    State = _state, 
                    BulletTime = _bulletTime
                };
        }
    }
}