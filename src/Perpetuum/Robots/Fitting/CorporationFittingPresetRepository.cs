using System.Collections.Generic;
using Perpetuum.Groups.Corporations;

namespace Perpetuum.Robots.Fitting
{
    public class CorporationFittingPresetRepository : FittingPresetRepositoryBase
    {
        private readonly Corporation _corporation;

        public CorporationFittingPresetRepository(Corporation corporation)
        {
            _corporation = corporation;
        }

        public override FittingPreset Get(int id)
        {
            return Get(id, _corporation.Eid);
        }

        public override IEnumerable<FittingPreset> GetAll()
        {
            return GetAll(_corporation.Eid);
        }
    }
}