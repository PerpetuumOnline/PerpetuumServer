using System;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZonePBSFixOrphaned : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var pbsObjectsOnZone = request.Zone.Units.Where(u => u is IPBSObject).ToList();

                Logger.Info("processing " + pbsObjectsOnZone.Count + " pbs objects on zone:" + request.Zone.Id + " for orphan check.");

                foreach (var unit in pbsObjectsOnZone)
                {
                    try
                    {
                        var pbsObject = unit as IPBSObject;
                        if (pbsObject == null)
                        {
                            continue;
                        }

                        var statusPre = pbsObject.IsOrphaned;

                        var tmpNetwork = pbsObject.ConnectionHandler.NetworkNodes;
                        var orphanStatus = !tmpNetwork.Any(n => n is PBSDockingBase);
                    
                        if (statusPre == orphanStatus) continue;

                        Logger.Warning("Orphan status correcting: " + statusPre + "->" + orphanStatus + "  for "  + unit);
                        pbsObject.IsOrphaned = orphanStatus;
                        unit.Save();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Orphan check error occured with " + unit);
                        Logger.Exception(ex);
                    }
                }

                Logger.Info("processed " + pbsObjectsOnZone.Count + " pbs objects on zone:" + request.Zone.Id + " for orphan check.");
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}