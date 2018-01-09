using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Units;

namespace Perpetuum.Zones.CombatLogs
{
    public class CombatLogHelper
    {
        private readonly ICorporationManager _corporationManager;

        public CombatLogHelper(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public Dictionary<string, object> GetUnitInfo(Unit unit)
        {
            var character = unit.GetCharacter();
            return new Dictionary<string, object>
            {
                {k.characterID,character.Id},
                {k.nick,character.Nick},
                {k.corporation,_corporationManager.GetCorporationNameByMember(character)},
                {k.robot,unit.Definition},
                {k.position,unit.CurrentPosition}
            };
        }
    }
}