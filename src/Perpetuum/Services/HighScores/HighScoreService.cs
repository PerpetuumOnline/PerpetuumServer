using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;

namespace Perpetuum.Services.HighScores
{
    public class HighScoreService : IHighScoreService
    {
        public IEnumerable<HighScore> GetHighScores(DateTimeRange range)
        {
            const string qry = @"select top(50) 
                    h.characterid,sum(playerskilled) as pk 
                    from characterhighscore h join characters c on h.characterid=c.characterid
                    where 
					(select acclevel from accounts where accountid=c.accountid)=0 and
					date between @startDate and @endDate
                    group by h.characterid order by pk desc";

            return Db.Query().CommandText(qry)
                .SetParameter("@startDate", range.Start)
                .SetParameter("@endDate", range.End)
                .Execute()
                .Select(CreateHighScoreFromRecord).ToArray();
        }

        public HighScore GetCharacterHighScores(int characterID, DateTimeRange range)
        {
            const string qry = @"select characterid,sum(playerskilled) as pk
                    from characterhighscore
                    where 
                    characterid=@characterID and 
                    (date between @startDate and @endDate)
                     group by characterid order by pk desc";

            var record = Db.Query().CommandText(qry)
                .SetParameter("@startDate", range.Start)
                .SetParameter("@endDate", range.End)
                .SetParameter("@characterID", characterID)
                .ExecuteSingleRow();

            if ( record == null )
                return new HighScore(characterID,0);

            return CreateHighScoreFromRecord(record);
        }

        public void UpdateHighScore(Player killer, Unit victim)
        {
            var killedPlayers = 0;
            var killedNpcs = 0;

            if (victim is Player)
            {
                //exit if the target was an arkhe
                if (victim.ED == Robot.NoobBotEntityDefault)
                    return;

                killedPlayers = 1;
            }
            else if (victim is Npc)
            {
                killedNpcs = 1;
            }

            if (killedPlayers == 0 && killedNpcs == 0)
                return;

            var timeRange = DateTime.Now.ToRange(-TimeSpan.FromDays(30));

            var record = Db.Query().CommandText("addKill")
                .SetParameter("@characterID", killer.Character.Id)
                .SetParameter("@killedPlayers", killedPlayers)
                .SetParameter("@killedNPCs", killedNpcs)
                .SetParameter("@startDate",timeRange.Start)
                .SetParameter("@endDate", timeRange.End)
                .ExecuteSingleRow();

            var result = CreateHighScoreFromRecord(record);
            SendHighScoreToPlayer(killer.Character, result);
        }

        private static void SendHighScoreToPlayer(Character killer, HighScore highScore)
        {
            Message.Builder.SetCommand(Commands.GetMyHighScores).WithData(highScore.ToDictionary()).ToCharacter(killer).Send();
        }

        private static HighScore CreateHighScoreFromRecord(IDataRecord record)
        {
            var characterID = record.GetValue<int>(0);
            var playersKilled = record.GetValue<int>(1);
            return new HighScore(characterID, playersKilled);
        }
    }
}