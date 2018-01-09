using System;
using System.Collections.Generic;

namespace Perpetuum.Services.Standing
{
    public class StandingLogEntry
    {
        public int characterID;
        public long allianceEID;
        public double actual;
        public double change;
        public int? missionID;
        public DateTime date;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.date,date},
                {k.current,actual},
                {k.change,change},
                {k.allianceEID,allianceEID},
                {k.missionID,missionID},
            };
        }
    }
}