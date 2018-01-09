using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.CombatLogs
{
    public class CombatLogSaver : ICombatLogSaver
    {
        private readonly CombatLogHelper _combatLogHelper;

        public CombatLogSaver(CombatLogHelper combatLogHelper)
        {
            _combatLogHelper = combatLogHelper;
        }

        public void Save(IZone zone, Player owner, Unit killer,IEnumerable<CombatSummary> summaries)
        {
            var reportData = GenxyConverter.Serialize(CreateReportData(zone,owner,summaries));

            var reportId = Guid.NewGuid();
            Db.Query().CommandText("insert into killreports (id,date,data) values (@id,getdate(),@data)").SetParameter("@id",reportId).SetParameter("@data",reportData).ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            SaveParticipantsToDb(reportId, owner.Character,true, false);

            foreach (var summary in summaries)
            {
                var isKiller = summary.Source == killer;
                SaveParticipantsToDb(reportId,summary.Source.GetCharacter(), false, true,isKiller);
            }
        }

        private IEnumerable<KeyValuePair<string, object>> CreateReportData(IZone zone,Player owner,IEnumerable<CombatSummary> summaries)
        {
            var result = new Dictionary<string, object>
            {
                {k.zoneID,zone.Id},
                {k.victim,_combatLogHelper.GetUnitInfo(owner)},
            };

            var attackers = summaries.ToDictionary("a", summary => summary.ToDictionary());
            result.Add(k.attackers, attackers);
            return result;
        }

        private static void SaveParticipantsToDb(Guid reportId, Character character, bool isVictim, bool isAttacker, bool isKiller = false)
        {
            if (character.Id == 0)
                return;

            const string sqlInsertCmd = "insert into characterkillreports (characterid,reportid,victim,attacker,killer) values (@characterId,@reportId,@isVictim,@isAttacker,@isKiller)";
            Db.Query().CommandText(sqlInsertCmd)
                .SetParameter("@characterId",character.Id)
                .SetParameter("@reportId",reportId)
                .SetParameter("@isVictim",isVictim)
                .SetParameter("@isAttacker",isAttacker)
                .SetParameter("@isKiller",isKiller)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }
    }
}