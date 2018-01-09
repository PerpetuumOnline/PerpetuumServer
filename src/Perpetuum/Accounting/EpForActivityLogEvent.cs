using System;
using System.Collections.Generic;
using Perpetuum.Log;

namespace Perpetuum.Accounting
{
    public class EpForActivityLogEvent : ILogEvent
    {
        public EpForActivityType TransactionType { get; private set; }

        public EpForActivityLogEvent(EpForActivityType transactionType)
        {
            TransactionType = transactionType;
            Created = DateTime.Now;
        }

        public Account Account { get; set; }
        public int CharacterId { get; set; }
        public int RawPoints { get; set; }
        public int Points { get; set; }
        public double BoostFactor { get; set; }
        public DateTime Created { get; set; }

        public IDictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string, object>
            {
                {k.EpForActivityType, (int) TransactionType},
                {k.characterID, CharacterId},
                {k.rawPoints, RawPoints},
                {k.points, Points},
                {k.boostFactor, BoostFactor},
                {k.created, Created}
            };

            return d;
        }

    }
}