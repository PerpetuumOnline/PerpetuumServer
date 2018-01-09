using System.Collections.Generic;

namespace Perpetuum.Services.HighScores
{
    public struct HighScore
    {
        public readonly int characterID;
        public readonly int playersKilled;

        public HighScore(int characterID, int playersKilled)
        {
            this.characterID = characterID;
            this.playersKilled = playersKilled;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.characterID, characterID},
                {k.playersKilled, playersKilled},
            };
        }
    }
}