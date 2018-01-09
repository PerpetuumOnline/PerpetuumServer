using System;
using System.Collections.Generic;

namespace Perpetuum.Services.Sparks
{
    public class UnlockedSpark
    {
        public readonly int sparkId;
        public readonly bool active;
        public DateTime? activationTime;

        public UnlockedSpark(int sparkId, bool active, DateTime? activationTime)
        {
            this.sparkId = sparkId;
            this.active = active;
            this.activationTime = activationTime;
        }

        public Dictionary<string,object> ToDictionary()
        {
            var info = new Dictionary<string, object>
            {
                {k.sparkID, sparkId},
                {k.active, active},
            };

            if (activationTime != null)
            {
                info.Add(k.activationTime, ((DateTime)activationTime).AddMinutes(SparkHelper.SPARK_CHANGE_MINUTES) );
            }

            return info;
        }
    }
}