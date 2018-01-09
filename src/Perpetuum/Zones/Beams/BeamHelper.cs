using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;

namespace Perpetuum.Zones.Beams
{
    /// <summary>
    /// Helper functions to emit a beam into the zone
    /// </summary>
    public static class BeamHelper
    {
        private static readonly IDictionary<BeamType, int> _cacheBeamDelays = Database.CreateCache<BeamType, int>("beams", "id", "startdelay");
        private static readonly IDictionary<int, BeamType> _cacheBeamAssignments = Database.CreateCache<int, BeamType>("beamassignment", "definition", "beam");

        public static int GetBeamDelay(BeamType beamType)
        {
            return _cacheBeamDelays[beamType];
        }

        public static BeamType GetBeamByDefinition(int definition)
        {
            if (!_cacheBeamAssignments.ContainsKey(definition))
            {
                Logger.Warning("no beam defined for definition: " + definition);
                return BeamType.undefined;
            }

            return _cacheBeamAssignments[definition];
        }
    }
}