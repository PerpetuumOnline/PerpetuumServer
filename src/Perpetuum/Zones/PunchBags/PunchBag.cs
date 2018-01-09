using System.Collections.Generic;
using Perpetuum.Robots;

namespace Perpetuum.Zones.PunchBags
{
    public class PunchBag : Robot
    {
        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();

            result.Remove(k.head);
            result.Remove(k.chassis);
            result.Remove(k.leg);

            return result;
        }
    }
}