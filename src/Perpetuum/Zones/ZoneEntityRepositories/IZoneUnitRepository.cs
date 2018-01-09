using Perpetuum.Units;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public interface IZoneUnitRepository : IZoneUnitReader
    {
        void Insert(Unit unit,Position position,string syncPrefix,bool runtime);
        void Delete(Unit unit);
        void Update(Unit unit);
    }
}