using System.Collections.Generic;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Services.HighScores
{
    public interface IHighScoreService
    {
        IEnumerable<HighScore> GetHighScores(DateTimeRange range);
        HighScore GetCharacterHighScores(int characterID, DateTimeRange range);
        void UpdateHighScore(Player killer,Unit victim);
    }
}