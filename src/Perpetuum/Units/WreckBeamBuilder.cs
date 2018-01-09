using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Robots;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Units
{
    public class WreckBeamBuilder : IBeamBuilder
    {
        private readonly Unit _unit;

        public WreckBeamBuilder(Unit unit)
        {
            _unit = unit;
        }

        public Beam Build()
        {
            return Beam.NewBuilder()
                .WithType(GetWreckBeamType())
                .WithPosition(_unit.CurrentPosition)
                .WithState(BeamState.WreckSelect)
                .WithDuration(TimeSpan.FromSeconds(30))
                .Build();
        }

        private BeamType GetWreckBeamType()
        {
            var robot = _unit as Robot;
            var definition = robot?.GetRobotComponent(RobotComponentType.Leg).Definition ?? _unit.Definition;
            return BeamHelper.GetBeamByDefinition(definition);
        }
    }
}