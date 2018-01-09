using System;
using System.Collections.Generic;
using Perpetuum.GenXY;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterProfile
    {
        public Character character;
        public long rootEID;
        public string nick;
        public DateTime creation;
        public long defaultCorporation;
        public GenxyString avatar;
        public int raceID;
        public int majorID;
        public int schoolID;
        public int sparkID;
        public string moodMessage;
        public DateTime lastLoggedIn;
        public DateTime lastLogOut;
        public int totalMinsOnline;
        public int language;
        public bool blockTrades;
        public bool globalMute;
        public long corporationEid;
        public long? allianceEid;
        private bool trial = false;
        public int accountID;

        public Dictionary<string,object> ToDictionary()
        {
            var dictionary = new Dictionary<string,object>
            {
                {k.characterID, character.Id},
                {k.rootEID,rootEID},
                {k.nick,nick},
                {k.creation,creation},
                {k.defaultCorporation,defaultCorporation},
                {k.avatar, avatar},
                {k.raceID, raceID},
                {k.majorID, majorID},
                {k.schoolID, schoolID},
                {k.sparkID, sparkID},
                {k.moodMessage, moodMessage},
                {k.lastLoggedIn, lastLoggedIn},
                {k.lastLogOut, lastLogOut},
                {k.totalMinsOnline, totalMinsOnline},
                {k.language, language},
                {k.blockTrades, blockTrades},
                {k.globalMute, globalMute},
                {k.corporationEID, corporationEid},
                {k.allianceEID, allianceEid},
                {k.trial, trial},
            };

            return dictionary;
        }
    }
}