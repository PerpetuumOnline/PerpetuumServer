using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Services.Looting;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.Zones
{
    public static partial class ZoneExtensions
    {
        public static void UpdateUnitRelations(this IZone zone, Unit sourceUnit)
        {
            foreach (var targetUnit in zone.Units)
            {
                if (sourceUnit == targetUnit)
                    continue;

                sourceUnit.UpdateVisibilityOf(targetUnit);
                targetUnit.UpdateVisibilityOf(sourceUnit);

                if (zone.Configuration.Protected)
                    continue;

                var bSource = sourceUnit as IBlobableUnit;
                bSource?.BlobHandler.UpdateBlob(targetUnit);

                var bTarget = targetUnit as IBlobableUnit;
                bTarget?.BlobHandler.UpdateBlob(sourceUnit);
            }
        }

        public static Dictionary<string, object> GetBuildingsDictionaryForCharacter(this IZone zone, Character character)
        {
            var count = 0;
            var buildingsDict = zone.Units
                                    .NotOf().Types<Robot, LootContainer, MobileTeleport>().Type<ProximityProbeBase>()
                                    .Where(u =>
                                    {
                                        if (u is PBSDockingBase pbsDockingBase)
                                        {
                                            return pbsDockingBase.IsVisible(character);
                                        }
                                        else if (u is IPBSObject pbs)
                                        {
                                            return false; //pbs cuccok nem latszodnak
                                        }
                                        else if (u is Gate gate)
                                        {
                                            return gate.IsVisible(character);
                                        }
                                        return true;
                                    })
                                    .ToDictionary<Unit, string, object>(unit => "b" + count++, unit => unit.ToDictionary());

            var result = new Dictionary<string, object>
            {
                {k.zoneID, zone.Id},
                {k.buildings,buildingsDict}
            };

            return result;
        }


        public static IEnumerable<TeleportColumn> GetTeleportColumns(this IZone zone)
        {
            return zone.GetStaticUnits().OfType<TeleportColumn>();
        }

        public static IEnumerable<Unit> GetStaticUnits(this IZone zone)
        {
            return zone.Units.NotOf().Types<Robot,ProximityProbeBase>();
        }

        [NotNull]
        public static Unit GetUnitOrThrow(this IZone zone, long eid)
        {
            return zone.GetUnit(eid).ThrowIfNull(ErrorCodes.EntityNotFound);
        }

        [NotNull]
        public static T GetUnitOrThrow<T>(this IZone zone, long eid) where T:Unit
        {
            return zone.GetUnit(eid).ThrowIfNotType<T>(ErrorCodes.EntityNotFound);
        }

        public static bool IsAnyConstructibleWithinRadius2D(this IZone zone, Position position, int constructionRadius)
        {
            if (zone == null)
                return false;

            foreach (var unit in zone.Units)
            {
                var egg = unit as PBSEgg;
                
                int radius;
                if (egg != null)
                {
                    radius = egg.GetConstructionRadius();
                }
                else
                {
                    if (!unit.TryGetConstructionRadius(out radius))
                        continue;
                }
                
                var inRange = position.IsWithinRangeOf2D(unit.CurrentPosition, constructionRadius + radius);
                if (!inRange)
                    continue;
#if DEBUG
                Logger.Error("too close to:" + unit.ED.Name + " " + unit.Definition + " range:" + (constructionRadius + radius) + " distance:" + position.TotalDistance2D(unit.CurrentPosition));
#endif
                return true;
            }

            return false;
        }

        public static bool IsOverlappingWithCategory(this IZone zone, CategoryFlags categoryFlags,   Position position, int typeExclusiveRange)
        {
           
            var unitsInCategory = zone.Units.Where(u => u.IsCategory(categoryFlags) || u is PBSEgg).ToList();

            var isInRange =
            unitsInCategory.Any(u =>
            {
                var matchRange = u.ED.Config.typeExclusiveRange;

                var egg = u as PBSEgg;
                if (egg != null && egg.TargetPBSNodeDefault.CategoryFlags.IsCategory(categoryFlags))
                {
                    matchRange = egg.TargetPBSNodeDefault.Config.typeExclusiveRange;
                }

                if (matchRange == null) return false;

                var range = typeExclusiveRange + (int)matchRange;

                return position.IsInRangeOf2D(u.CurrentPosition, range);
            });

            return isInRange;
        }

        public static bool IsUnitWithCategoryInRange(this IZone zone, CategoryFlags categoryFlags, Position position, int range)
        {
            var unitsInCategory = zone.Units.Where(u => u.IsCategory(categoryFlags) || u is PBSEgg).ToList();

            foreach (var unit in unitsInCategory)
            {

                var egg = unit as PBSEgg;

                if (egg != null)
                {
                    if (egg.TargetPBSNodeDefault.CategoryFlags.IsCategory(categoryFlags))
                    {
                        if (position.IsInRangeOf2D(egg.CurrentPosition, range))
                        {
                            return true;
                        }
                    }
                    
                }

                if (position.IsInRangeOf2D(unit.CurrentPosition, range))
                {
                    return true;
                }

            }

            return false;
        }

        public static IEnumerable<Unit> GetUnitsWithinRange2D(this IZone zone,Position position,double range)
        {
            if (zone == null)
                return Enumerable.Empty<Unit>();

            return zone.Units.WithinRange2D(position, range);
        }

        public static IEnumerable<Unit> GetUnits(this IEnumerable<IZone> zones)
        {
            return zones.SelectMany(z => z.Units);
        }

        public static IEnumerable<T> GetUnits<T>(this IEnumerable<IZone> zones) where T:Unit
        {
            return zones.SelectMany(z => z.Units.OfType<T>());
        }
    }
}
