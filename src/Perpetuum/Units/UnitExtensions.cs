using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Players.ExtensionMethods;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.PBS;

namespace Perpetuum.Units
{
    public static class UnitExtensions
    {
        public static void AddDirectThreat(this Unit unit, Unit hostile, double threatValue)
        {
            var v = unit.GetVisibility(hostile);
            if (v == null)
            {
                // force update
                unit.UpdateVisibilityOf(hostile);
            }

            unit.AddThreat(hostile,new Threat(ThreatType.Direct, threatValue));
        }

        public static void AddThreat(this Unit unit,Unit hostile,Threat threat)
        {
            if (unit is Npc npc && npc.CanAddThreatTo(hostile, threat))
                npc.AddThreat(hostile, threat, true);
        }

        public static bool TryGetConstructionRadius(this Unit unit,out int radius)
        {
            radius = 0;

            if (unit?.ED.Config.constructionRadius == null)
                return false;

            radius = (int) unit.ED.Config.constructionRadius;
            return true;
        }

        public static double GetCoreConsumption(this Unit unit)
        {
            return unit.ED.Config.CoreConsumption;
        }

        public static int GetItemWorkRangeOrDefault(this Unit unit)
        {
            if (unit == null || unit.ED.Config.item_work_range == null)
                return 0;

            return (int) unit.ED.Config.item_work_range;
        }


        public static int GetBlockingRadiusOrDefault(this Unit unit)
        {
            if (unit == null || unit.ED.Config.blockingradius == null)
                return 0;

            return (int)unit.ED.Config.blockingradius;
        }


        public static int GetCycleTimeMs(this Unit unit)
        {
            if (unit == null)
                return 0;

            if (unit.ED.Config.cycle_time != null)
                return (int) unit.ED.Config.cycle_time;

            return 30000;
        }


        public static bool IsCoreFull(this Unit unit)
        {
            if (unit == null) return true;

            var coreFillRate = unit.Core.Ratio(unit.CoreMax);

            return 1 - coreFillRate <= double.Epsilon;

        }

        public static int GetConstructionRadius(this Unit unit)
        {
            if (unit == null)
                return 0;

            if (unit.ED.Config.constructionRadius != null)
                return (int) unit.ED.Config.constructionRadius;

            Logger.Error("consistency error. no construction radius was defined for definition: " + unit.Definition + " " + unit.ED.Name);
            return 10;
        }



        public static void AddEffectsDebugInfo(this Unit unit, IDictionary<string,object> info)
        {
            var effects = new Dictionary<string, object>();
            var counter = 0;
            foreach (var effect in unit.EffectHandler.Effects)
            {
                effects.Add("e" + counter++, effect.Type.ToString());
            }

            if (counter > 0)
            {
                info.Add("effects", effects);
            }
        }

        public static IDictionary<string, object> GetMiniDebugInfo(this Unit unit)
        {
            return new Dictionary<string, object>
            {
                {k.name, unit.Name},
                {k.eid, unit.Eid}, 
                {k.definitionName, unit.ED.Name}, 
                {k.owner, unit.Owner}, 
                {k.state, unit.States.ToString()}
            };
        }

        public static int GetTransmitRadius(this Unit unit)
        {
            if (unit == null)
                return 0;

            if (unit.ED.Config.transmitradius != null)
                return (int) unit.ED.Config.transmitradius;

            Logger.Error("consistency error. no transmitRadius was defined for definition: " + unit.Definition + " " + unit.ED.Name);
            return 0;
        }

        public static double GetCoreTransferred(this Unit unit)
        {
            if (unit == null)
                return 1;

            if (unit.ED.Config.coreTransferred != null)
                return (double)unit.ED.Config.coreTransferred;

            Logger.Error("coreTransferred not defined for " + unit);
            return 100;

            
        }

        public static double GetTransferEfficiency(this Unit unit)
        {
            if (unit.ED.Config.transferEfficiency != null)
                return ((double) unit.ED.Config.transferEfficiency).Clamp();

            Logger.Error("transferEfficiency not defined for " + unit);
            return 0.8;

        }

        public static void KillAll(this IEnumerable<Unit> units,Unit killer = null)
        {
            foreach (var unit in units)
            {
                unit.Kill(killer);
            }
        }

        public static void SpreadAssistThreatToNpcs(this Unit unit, Unit assistant,Threat threat)
        {
            if ( unit == null || assistant == null)
                return;

            foreach (var npc in unit.GetWitnessUnits<Npc>())
            {
                npc.AddAssistThreat(assistant,unit,threat);
            }
        }
        
        public static bool IsPlayer(this Unit unit) { return unit is Player; }


        public static void SendPacketToWitnessPlayers(this Unit source, Packet packet, bool sendSelf = false)
        {
            if (sendSelf)
            {
                (source as Player)?.Session.SendPacket(packet);
            }

            source.GetWitnessUnits<Player>().SendPacket(packet);
        }


        public static IEnumerable<T> WithinRange<T>(this IEnumerable<T> units,Position position,double distance) where T : Unit
        {
            return units.Where(unit => unit.IsInRangeOf3D(position, distance));
        }

        public static IEnumerable<T> WithinRange2D<T>(this IEnumerable<T> units,Position position,double distance) where T : Unit
        {
            return units.Where(unit => unit.CurrentPosition.IsInRangeOf2D(position, distance));
        }

        public static IEnumerable<T> WithinArea<T>(this IEnumerable<T> units, Area area) where T : Unit
        {
            return units.Where(unit => area.Contains(unit.CurrentPosition));
        }

        public static bool IsPlaceableOutsideOfDockingbaseRange(this Unit unit)
        {
            return PBSHelper.IsPlaceableOutsideOfBase(unit.ED.CategoryFlags);
        }

        [CanBeNull]
        public static T GetNearestUnit<T>(this IEnumerable<T> units, Position position) where T : Unit
        {
            var nearest = double.MaxValue;
            T nearestUnit = null;

            foreach (var unit in units)
            {
                var d = position.SqrDistance3D(unit.CurrentPosition);
                if (d > nearest)
                    continue;

                nearest = d;
                nearestUnit = unit;
            }

            return nearestUnit;
        }

        public static IEnumerable<T> GetAllByCategoryFlags<T>(this IEnumerable<T> units, CategoryFlags cf) where T:Unit
        {
            return units.Where(u => u.ED.CategoryFlags.IsCategory(cf)).ToArray();
        }
    }
}