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
    public class ZoneEffectReader
    {
        private readonly IZone _zone;

        public ZoneEffectReader(IZone zone)
        {
            _zone = zone;
        }

        private ZoneEffect CreateZoneEffectFromRecord(IDataRecord record)
        {
            try
            {
                var value = record.GetValue<int>("effectid");
                var effectType = EnumHelper.GetEnum<EffectType>(value);
                var config = new ZoneEffect(_zone, effectType);
                return config;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
            return null;
        }

        public IEnumerable<ZoneEffect> GetZoneEffects()
        {
            var zoneEffects = Db.Query().CommandText("SELECT zoneid, effectid FROM zoneeffects WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", _zone.Id)
                .Execute()
                .Select(CreateZoneEffectFromRecord);

            return zoneEffects.Where(z => z != null);
        }
    }
}
