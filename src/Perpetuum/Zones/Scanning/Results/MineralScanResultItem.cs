using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.Scanning.Results
{
    /// <summary>
    /// Item generated from a mineral scan result
    /// </summary>
    public class MineralScanResultItem : Item
    {
        private static readonly EntityDefault _entityDefault = EntityDefault.GetByName(DefinitionNames.MINING_PROBE_RESULT_TILE);
       
        private readonly IDynamicProperty<IDictionary<string, object>> _info;
        
        public MineralScanResultItem()
        {
            _info = DynamicProperties.GetProperty(k.probeInfo, () => (IDictionary<string, object>) new Dictionary<string, object>());
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.AddRange(_info.Value);
            return result;
        }

        [CanBeNull]
        public MineralScanResult ToScanResult()
        {
            var dictionary = _info.Value;
            if (dictionary.IsNullOrEmpty())
                return null;

            var result = new MineralScanResult();

            result.ScanAccuracy = dictionary.GetValue<double>(k.scanAccuracy);
            result.FoundAny = true;
            result.Area = dictionary.GetValue<Area>(k.area);
            result.MaterialType = dictionary.GetValue<string>(k.mineral).ToMaterialType();
            result.ZoneId = dictionary.GetValue<int>(k.zone);
            result.Creation = dictionary.GetValue<DateTime>(k.date);
           
            return result;
        }

        public static MineralScanResultItem Create()
        {
            return (MineralScanResultItem)Factory.CreateWithRandomEID(_entityDefault);
        }
    }
}
