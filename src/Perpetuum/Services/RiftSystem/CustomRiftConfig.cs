using Perpetuum.Collections;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.RiftSystem
{
    public interface ICustomRiftConfigReader
    {
        IEnumerable<CustomRiftConfig> RiftConfigs { get; }
        CustomRiftConfig GetById(int id);
    }

    public class CustomRiftConfigReader : ICustomRiftConfigReader
    {
        public IEnumerable<CustomRiftConfig> RiftConfigs
        {
            get { return _riftconfigs.Values; }
        }
        private readonly IDictionary<int, CustomRiftConfig> _riftconfigs;

        public CustomRiftConfigReader()
        {
            _riftconfigs = Database.CreateCache<int, CustomRiftConfig>("riftconfigs", "id", r =>
            {
                var id = r.GetValue<int>("id");
                var name = r.GetValue<string>("name");
                var destinationGroupId = r.GetValue<int>("destinationGroupId");
                var lifetimeSeconds = r.GetValue<int?>("lifespanSeconds") ?? 0;
                var maxUses = r.GetValue<int?>("maxUses") ?? -1;
                var catExcludeGroupId = r.GetValue<int?>("categoryExclusionGroupId") ?? -1;

                var destinations = GetDestinations(destinationGroupId);
                var excludedCats = GetExclusionCategories(catExcludeGroupId);
                return new CustomRiftConfig(id, name, destinations, maxUses, TimeSpan.FromSeconds(lifetimeSeconds), excludedCats);
            });
        }

        private WeightedCollection<Destination> GetDestinations(int destinationGroupId)
        {
            var group = Db.Query().CommandText(
                    @"SELECT id, groupId, zoneId, x, y, weight FROM riftdestinations WHERE groupId=@groupId 
                    AND zoneId IN (SELECT id FROM zones WHERE enabled=1 AND zoneId=id);")
                    .SetParameter("@groupId", destinationGroupId)
                    .Execute()
                    .Select((record) =>
                    {
                        var groupId = record.GetValue<int>("groupId");
                        var zoneId = record.GetValue<int>("zoneId");
                        var x = record.GetValue<int?>("x");
                        var y = record.GetValue<int?>("y");
                        var weight = record.GetValue<int>("weight");

                        return new Destination(groupId, zoneId, x, y, weight);
                    });
            var collection = new WeightedCollection<Destination>();
            foreach (var destination in group)
            {
                collection.Add(destination, destination.Weight);
            }
            return collection;
        }

        private CategoryFlags[] GetExclusionCategories(int categoryGroupId)
        {
            var group = Db.Query().CommandText(
                    @"SELECT id, groupId, category FROM categorygroups WHERE groupId=@groupId;")
                    .SetParameter("@groupId", categoryGroupId)
                    .Execute()
                    .Select((record) =>
                    {
                        var catValue = record.GetValue<long>("category");
                        return EnumHelper.GetEnum<CategoryFlags>(catValue);
                    });
            return group.ToArray();
        }

        public CustomRiftConfig GetById(int id)
        {
            return RiftConfigs.FirstOrDefault(r => r.Id == id);
        }
    }

    public static class CustomRiftSpawner
    {
        public static bool TrySpawnRift(CustomRiftConfig config, IZoneManager zoneManager, int sourceZoneId, Position sourcePosition, Func<DespawningTargettedPortal> riftFactory)
        {
            var zone = zoneManager.GetZone(sourceZoneId);
            var targetDestination = config.GetDestination();
            if (targetDestination == null)
            {
                Logger.Error($"TargettedRift failed to spawn with config {config} \ntargetDestination == null");
                return false;
            }

            var zoneTarget = zoneManager.GetZone(targetDestination.ZoneId);
            if (zoneTarget == null)
            {
                Logger.Error($"TargettedRift failed to spawn with config {config} \nzoneTarget == null");
                return false;
            }

            var targetPos = targetDestination.GetPosition(zoneTarget);
            var rift = riftFactory();
            rift.SetTarget(zoneTarget, targetPos);
            rift.SetConfig(config);
            rift.AddToZone(zone, sourcePosition, ZoneEnterType.NpcSpawn);

            Logger.Info(string.Format("Rift spawned on zone {0} {1} ({2})", zone.Id, rift.ED.Name, rift.CurrentPosition));
            return true;
        }
    }

    public class CustomRiftConfig
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public TimeSpan Lifespan { get; private set; }
        public int MaxUses { get; private set; }
        public CategoryFlags[] ExcludeClasses { get; private set; }
        private readonly WeightedCollection<Destination> _destinations;

        public CustomRiftConfig(int id, string name, WeightedCollection<Destination> destinations, int maxUses, TimeSpan lifespan, CategoryFlags[] excludeClasses)
        {
            Id = id;
            Name = name;
            _destinations = destinations;
            MaxUses = maxUses;
            Lifespan = lifespan;
            ExcludeClasses = excludeClasses;
        }

        public bool IsDespawning { get { return !Lifespan.Equals(TimeSpan.Zero); } }
        public bool InfiniteUses { get { return MaxUses < 0; } }

        public bool IsExcluded(CategoryFlags category)
        {
            return category.IsAny(ExcludeClasses);
        }

        [CanBeNull]
        public Destination GetDestination()
        {
            return _destinations.GetRandom();
        }

        public override string ToString()
        {
            return $"CustomRiftConfig:id:{Id};n:{Name};l:{Lifespan};u:{MaxUses};";
        }
    }


    public class Destination
    {
        private Position Location { get; set; }
        public bool IsRandomLocation { get { return Location.Equals(Position.Empty); } }
        public int ZoneId { get; private set; }
        public int Weight { get; private set; }
        public int Group { get; private set; }

        public Destination(int groupId, int zoneId, int? x, int? y, int weight = 1)
        {
            Group = groupId;
            ZoneId = zoneId;
            Location = x == null || y == null ? Position.Empty : new Position(x ?? 0, y ?? 0);
            Weight = weight;
        }

        public Position GetPosition(IZone zone)
        {
            if (IsRandomLocation)
            {
                var randomFinder = new RandomPassablePositionFinder(zone);
                if (randomFinder.Find(out Position random))
                {
                    return random;
                }
            }
            var closestFinder = new ClosestWalkablePositionFinder(zone, Location);
            if (closestFinder.Find(out Position closest))
            {
                return closest;
            }
            return Location;
        }
    }
}
