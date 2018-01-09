using System.Collections.Generic;

namespace Perpetuum.Services.Sparks
{
    public class SparkExtension
    {
        public int SparkId { get; set; }
        public Extension Extension { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.sparkID, SparkId},
                {k.extension, Extension.id},
                {k.extensionLevel, Extension.level}
            };
        }
    }
}