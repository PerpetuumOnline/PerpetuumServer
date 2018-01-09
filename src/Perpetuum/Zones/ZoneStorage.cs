using Perpetuum.Containers.SystemContainers;

namespace Perpetuum.Zones
{
    public class ZoneStorage : SystemContainer
    {
        public static ZoneStorage Get(ZoneConfiguration configuration)
        {
            return (ZoneStorage) GetByName(GetNameByZoneId(configuration.Id));
        }

        private static string GetNameByZoneId(int zoneId)
        {
            return $"es_zone_{zoneId}_storage";
        }
    }
}