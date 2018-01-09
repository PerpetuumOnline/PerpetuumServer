using System;
using System.Collections.Generic;
using Perpetuum.Log;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketTaxChangeLogEvent : ILogEvent
    {
        public MarketTaxChangeLogEvent()
        {
            EventTime = DateTime.Now;
        }

        public DateTime EventTime { get; set; }
        public long Owner { get; set; }
        public int CharacterId { get; set; }
        public long BaseEid { get; set; }
        public double ChangeFrom { get; set; }
        public double ChangeTo { get; set; }
        

        public IDictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string, object>
            {
                {k.date, EventTime}, 
                {k.characterID, CharacterId}, 
                {k.baseEID, BaseEid}, 
                {k.from, ChangeFrom}, 
                {k.to, ChangeTo}, 
                
            };

            return d;
        }
    }
}
