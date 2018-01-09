using System;
using System.Collections.Generic;
using System.Numerics;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Services.MissionEngine.MissionDataCacheObjects
{
    public interface ITargetSelectionValidator
    {
        bool ValidateSelectedPoints(Vector2 start, Vector2 end);
    }

    public class TwoPointIslandValidator : ITargetSelectionValidator
    {
        private readonly IZone _zone;

        public TwoPointIslandValidator(IZone zone)
        {
            _zone = zone;
        }

        public bool ValidateSelectedPoints(Vector2 start, Vector2 end)
        {
            foreach (var vec2 in start.LineTo(end))
            {
                var bi = _zone.Terrain.Blocks.GetValue(vec2);
                if (bi.Island)
                    return false;
            }

            return true;
        }
    }


    public class TargetSelectionValidator
    {
        private readonly IEnumerable<IZone> _usedZones;
        private readonly Lazy<Dictionary<int, ITargetSelectionValidator>> _validators;

        public TargetSelectionValidator(IEnumerable<IZone> usedZones)
        {
            _usedZones = usedZones;
            _validators = new Lazy<Dictionary<int, ITargetSelectionValidator>>(LoadAllValidators);
        }

        private Dictionary<int, ITargetSelectionValidator> LoadAllValidators()
        {
            var v = new Dictionary<int, ITargetSelectionValidator>();
            foreach (var zone in _usedZones)
            {
                v[zone.Id] = new TwoPointIslandValidator(zone);
            }

            return v;
        }

        public bool IsTargetSelectionValid(IZone zone,Position source, Position target)
        {
            if (_validators.Value.TryGetValue(zone.Id, out ITargetSelectionValidator validator))
            {
                return validator.ValidateSelectedPoints(source.ToVector2(), target.ToVector2());
            }

            return true;
        }

        private static readonly int[] _manualConfig = {0, 1, 2, 6, 7, 8, 5, 3, 4};

        public static TargetSelectionValidator CreateValidator(IZoneManager zoneManager)
        {
            var initValues = new List<IZone>();

            foreach (var zoneId in _manualConfig)
            {
                var zone = zoneManager.GetZone(zoneId);
                if (zone == null)
                    continue;

                initValues.Add(zone);
            }

            return new TargetSelectionValidator(initValues);
        }
    }

}
