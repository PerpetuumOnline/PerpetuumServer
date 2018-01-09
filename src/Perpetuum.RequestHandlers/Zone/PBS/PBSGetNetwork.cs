using System.Collections.Generic;
using System.Linq;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Units;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSGetNetwork : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var isAdmin = request.Session.AccessLevel.IsAdminOrGm() && request.Data.ContainsKey(k.all);
            var character = request.Session.Character;

            Corporation.GetCorporationEidAndRoleFromSql(character,out long corporationEid,out CorporationRole role);

            DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);

            if (!isAdmin)
            {
                role.IsAnyRole(CorporationRole.viewPBS, CorporationRole.CEO, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            }

            var result = new Dictionary<string, object>
                             {
                                 {k.zoneID, request.Zone.Id},
                             };

            var ourBaseOnZone = false;
            var network = new Dictionary<string, object>();

            var ourNodes = new List<Unit>();
            var orphanedNodes = new List<Unit>();

            foreach (var unit in request.Zone.Units.Where(o => o is IPBSObject))
            {
                var pbsObject = (IPBSObject) unit;

                if (isAdmin || unit.Owner == corporationEid || pbsObject.IsOrphaned)
                {

                    if (unit.Owner == corporationEid)
                    {
                        ourNodes.Add(unit);

                        if (unit is PBSDockingBase)
                        {
                            ourBaseOnZone = true;
                        }

                        continue;

                    }

                    if (pbsObject.IsOrphaned)
                    {
                        orphanedNodes.Add(unit);
                    }
                }
                
            
            }

            var counter = 0;
            foreach (var node in ourNodes)
            {
                network.Add("c"+counter++, node.ToDictionary());
            }

            //if we own a docking base on the zone we'll see the orphaned nodes regardless of the owner
            if (ourBaseOnZone)
            {
                foreach (var orphanedNode in orphanedNodes)
                {
                    network.Add("c"+counter++, orphanedNode.ToDictionary());
                }
            }

            result.Add(k.buildings, network);

            Message.Builder.FromRequest(request).WithData(result).Send();
        }


       
    }
}
