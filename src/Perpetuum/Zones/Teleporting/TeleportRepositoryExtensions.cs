using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Teleporting
{
    public static class TeleportRepositoryExtensions
    {
        public static IDictionary<string, object> ToDictionary(this IEnumerable<TeleportDescription> descriptions)
        {
            return descriptions.ToDictionary("t", td => td.ToDictionary());
        }

        public static IEnumerable<TeleportDescription> SelectMany(this ITeleportDescriptionRepository descriptionRepository, IEnumerable<int> descriptionIDs)
        {
            var descriptions = descriptionRepository.GetAll().ToDictionary(d => d.id);
            foreach (var id in descriptionIDs)
            {
                var d = descriptions.GetOrDefault(id);
                if (d != null)
                    yield return d;
            }
        }
    }
}