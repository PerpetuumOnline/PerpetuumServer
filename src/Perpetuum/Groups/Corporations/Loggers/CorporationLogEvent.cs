using Perpetuum.Accounting.Characters;
using Perpetuum.Log;

namespace Perpetuum.Groups.Corporations.Loggers
{
    public class CorporationLogEvent : ILogEvent
    {
        public CorporationLogType Type { get; set; }
        public Corporation Corporation { get; set; }
        public Character Issuer { get; set; }
        public Character Member { get; set; }
    }
}