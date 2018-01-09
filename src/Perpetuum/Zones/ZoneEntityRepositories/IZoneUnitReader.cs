using System.Collections.Generic;
using Perpetuum.Units;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public interface IZoneUnitReader
    {
        Dictionary<Unit,Position> GetAll();
    }
}