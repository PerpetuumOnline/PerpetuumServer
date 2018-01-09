using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Robots.Fitting
{
    public class CharacterFittingPresetRepository : FittingPresetRepositoryBase
    {
        private readonly Character _character;

        public CharacterFittingPresetRepository(Character character)
        {
            _character = character;
        }

        public override void Insert(FittingPreset preset)
        {
            preset.Owner = _character.Eid;
            base.Insert(preset);
        }

        public override FittingPreset Get(int id)
        {
            return Get(id, _character.Eid);
        }

        public override IEnumerable<FittingPreset> GetAll()
        {
            return GetAll(_character.Eid);
        }
    }
}