using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSRenameNode : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.eid);
                var character = request.Session.Character;
                var name = request.Data.GetOrDefault<string>(k.name);

                var sourceUnit = request.Zone.GetUnitOrThrow(sourceEid);
                var sourceNode = sourceUnit.ThrowIfNotType<IPBSObject>(ErrorCodes.DefinitionNotSupported);

                if (!request.Session.AccessLevel.IsAdminOrGm())
                {
                    sourceNode.CheckAccessAndThrowIfFailed(character);
                }

                sourceUnit.Name = name;
                sourceUnit.Save();

                Transaction.Current.OnCommited(() =>
                {
                    sourceNode.SendNodeUpdate();
                    Logger.Info($"PBSRenameNode (issuer = {character.Id}) {sourceUnit}");
                    Message.Builder.FromRequest(request).WithData(sourceUnit.ToDictionary()).Send();
                });
                
                scope.Complete();
            }
        }
    }
}