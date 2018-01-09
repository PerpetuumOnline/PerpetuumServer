using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.EnergyWell;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSSetOnline : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.eid);
                var character = request.Session.Character;
                var state = request.Data.GetOrDefault<int>(k.state) == 1;

                var sourceUnit = request.Zone.GetUnitOrThrow(sourceEid);
                var sourceNode = (sourceUnit as IPBSObject).ThrowIfNull(ErrorCodes.DefinitionNotSupported);

                sourceNode.IsFullyConstructed().ThrowIfFalse(ErrorCodes.ObjectNotFullyConstructed);
                sourceNode.CheckAccessAndThrowIfFailed(character);

                if (PBSHelper.IsOfflineOnReinforce(sourceUnit))
                {
                    //ezeket nem lehet kapcsolgatni ha reinforceban vannak
                    sourceNode.ReinforceHandler.CurrentState.IsReinforced.ThrowIfTrue(ErrorCodes.NotPossibleDuringReinforce);
                }

                if (sourceNode.OnlineStatus != state)
                {
                    if (state)
                    {
                        //be akarunk valamit kapcsolni
                        //ez a node akkor mehet csak onlineba ha nem rothadt ki alatta a mineral mar 1x
                        var energyWell = sourceUnit as PBSEnergyWell;
                        energyWell?.IsDepleted.ThrowIfTrue(ErrorCodes.EnergyWellDepleted);
                    }

                    sourceNode.SetOnlineStatus(state, true);
                    sourceUnit.Save();

                    Transaction.Current.OnCommited(() =>
                    {
                        sourceNode.SendNodeUpdate();
                        var logType = state ? PBSLogType.online : PBSLogType.offline;
                        PBSHelper.WritePBSLog(logType, sourceEid, sourceUnit.Definition, sourceUnit.Owner, character.Id, background: false, zoneId: request.Zone.Id);
                    });
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}