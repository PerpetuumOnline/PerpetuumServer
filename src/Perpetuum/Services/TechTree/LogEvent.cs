using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.TechTree
{
    public class LogEvent
    {
        public LogEvent(LogType type,Character character)
        {
            Type = type;
            Character = character;
            Points = Points.Empty;
        }

        public LogType Type { get; private set; }
        public Character Character { get; private set; }
        public int Definition { get; set; }
        public int Quantity { get; set; }
        public long? CorporationEid { get; set; }
        public Points Points { get; set; }
        public DateTime Created { get; set; }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.type, (int) Type}, 
                    {k.characterID, Character.Id}, 
                    {k.definition, Definition}, 
                    {k.quantity, Quantity}, 
                    {k.corporationEID, CorporationEid ?? 0L}, 
                    {k.points, Points.ToDictionary()},
                    {k.date,Created}
                };
        }
    }

    public class CharacterTechTreeLogger : TechTreeLogger
    {
        private readonly Character _character;

        public CharacterTechTreeLogger(Character character)
        {
            _character = character;
        }

        protected override IEnumerable<IDataRecord> GetLogEventRecords(DateTime from, DateTime to)
        {
            return Db.Query().CommandText("select * from techtreelog where character = @characterId and (created between @from and @to)")
                .SetParameter("@characterId",_character.Id)
                .SetParameter("@from",from)
                .SetParameter("@to",to)
                .Execute();
        }
    }

    public class CorporationTechTreeLogger : TechTreeLogger
    {
        private readonly long _corporationEid;

        public CorporationTechTreeLogger(long corporationEid)
        {
            _corporationEid = corporationEid;
        }

        protected override IEnumerable<IDataRecord> GetLogEventRecords(DateTime from, DateTime to)
        {
            return Db.Query().CommandText("select * from techtreelog where corporationEid = @corporationEid and (created between @from and @to)")
                          .SetParameter("@corporationEid",_corporationEid)
                          .SetParameter("@from",from)
                          .SetParameter("@to",to).Execute();
        }
    }

}