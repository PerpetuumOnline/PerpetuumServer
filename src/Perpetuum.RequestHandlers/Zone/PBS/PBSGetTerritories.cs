using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSGetTerritories : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var result = GenerateTerritoryDictionary(request.Zone,character);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }

        private Dictionary<string, object> GenerateTerritoryDictionary(IZone zone,Character character)
        {
            var result = new Dictionary<string, object> { { k.zoneID, zone.Id } };

            Corporation.GetCorporationEidAndRoleFromSql(character, out long corporationEid, out CorporationRole role);

            var counter = 0;
            var networks = new Dictionary<string, object>();

            foreach (var pbsDockingBase in zone.Units.OfType<PBSDockingBase>())
            {
                if (pbsDockingBase.NetworkMapVisibility == PBSDockingBaseVisibility.open)
                {
                    var oneNetwork = pbsDockingBase.GetTerritorialDictionary();
                    networks.Add("m" + counter++, oneNetwork);
                }

                if (pbsDockingBase.NetworkMapVisibility == PBSDockingBaseVisibility.corporation)
                {
                    if (pbsDockingBase.Owner == corporationEid)
                    {
                        var oneNetwork = pbsDockingBase.GetTerritorialDictionary();
                        networks.Add("m" + counter++, oneNetwork); 
                    }
                }

                if (pbsDockingBase.NetworkMapVisibility == PBSDockingBaseVisibility.hidden)
                {
                    if (pbsDockingBase.Owner == corporationEid)
                    {
                        if (role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.viewPBS))
                        {
                            var oneNetwork = pbsDockingBase.GetTerritorialDictionary();
                            networks.Add("m" + counter++, oneNetwork);
                        }
                    }
                }
            }

            result.Add("networks", networks);
            return result;
        }
    }
}