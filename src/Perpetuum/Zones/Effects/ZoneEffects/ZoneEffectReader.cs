using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    /// <summary>
    /// DB querying object for ZoneEffects
    /// </summary>
    public static class ZoneEffectReader
    {
        private static ZoneEffect CreateZoneEffectFromRecord(IDataRecord record)
        {
            try
            {
                var zoneId = record.GetValue<int>("zoneid");
                var effectId = record.GetValue<int>("effectid");
                var effectType = EnumHelper.GetEnum<EffectType>(effectId);
                var config = new ZoneEffect(zoneId, effectType, true); // TODO new bool col for isPlayerOnly
                return config;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
            return null;
        }

        public static IEnumerable<ZoneEffect> GetStaticZoneEffects(IZone zone)
        {
            var zoneEffects = Db.Query().CommandText("SELECT zoneid, effectid FROM zoneeffects WHERE zoneid=@zoneId")
                .SetParameter("@zoneId", zone.Id)
                .Execute()
                .Select(CreateZoneEffectFromRecord);

            return zoneEffects.Where(z => z != null);
        }
    }
}
