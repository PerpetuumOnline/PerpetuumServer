using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Groups.Corporations.Applications
{
    public static class CorporationApplicationExtensions
    {
        public static IEnumerable<CorporationApplication> GetApplicationsByCharacter(this PrivateCorporation corporation,Character character)
        {
            return corporation.GetApplications().Where(a => a.Character == character);
        }

        public static IEnumerable<CorporationApplication> GetApplications(this PrivateCorporation corporation)
        {
            return CorporationApplication.GetAllByCorporation(corporation);
        }

        public static IEnumerable<CorporationApplication> GetCorporationApplications(this Character character)
        {
            return CorporationApplication.GetAllByCharacter(character);
        }

        public static void DeleteAll(this IEnumerable<CorporationApplication> applications)
        {
            foreach (var application in applications)
            {
                application.DeleteFromDb();
            }
        }

        public static IDictionary<string, object> ToDictionary(this IEnumerable<CorporationApplication> applications)
        {
            return applications.ToDictionary("a", a => a.ToDictionary());
        }
    }
}