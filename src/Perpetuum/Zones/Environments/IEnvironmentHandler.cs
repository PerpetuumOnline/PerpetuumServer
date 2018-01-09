using System.Collections.Generic;

namespace Perpetuum.Zones.Environments
{
    public interface IEnvironmentHandler
    {
        List<int> ListEnvironmentDescriptions();
        ErrorCodes CollectEnvironmentFromPosition(Position getServerPosition, int range, int turns, out EntityEnvironmentDescription environmentDescription);
        ErrorCodes SampleEnvironment(long eid, int range, out Dictionary<string, object> result);
    }
}