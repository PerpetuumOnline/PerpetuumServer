using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.MissionEngine.TransportAssignments
{
    public static class TransportAssignmentExtensions
    {
        public static IDictionary<string, object> ToDictionary(this IEnumerable<TransportAssignment> infos, bool addPrivateInfo, Character volunteer)
        {
            return infos.ToDictionary("t", i => addPrivateInfo && (volunteer == i.volunteercharacter) ? i.ToPrivateDictionary() : i.ToDictionary());
        }
    }
}