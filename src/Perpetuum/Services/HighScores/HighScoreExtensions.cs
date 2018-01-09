using System.Threading.Tasks;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Services.HighScores
{
    public static class HighScoreExtensions
    {
        public static Task UpdateHighScoreAsync(this IHighScoreService service,Player killer, Unit victim)
        {
            return Task.Run(() => service.UpdateHighScore(killer,victim));
        }
    }
}