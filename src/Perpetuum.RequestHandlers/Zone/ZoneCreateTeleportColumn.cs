using System.Linq;
using System.Transactions;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneCreateTeleportColumn : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var x = request.Data.GetOrDefault(k.x, (double) -1);
                var y = request.Data.GetOrDefault(k.y, (double) -1);

                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);
                if (x < 0 || y < 0)
                {
                    x = player.CurrentPosition.intX + 0.5;
                    y = player.CurrentPosition.intY + 0.5;
                }

                //optionals
                var definition = request.Data.GetOrDefault<int>(k.definition);
                if (definition == 0)
                    definition = request.Zone.Configuration.TeleportColumn.Definition;

                var position = request.Zone.FixZ(new Position(x, y).Center);
                position.IsValid(request.Zone.Size).ThrowIfFalse(ErrorCodes.IllegalPosition);

                var eid = (long)request.Data.GetOrDefault<int>(k.eid); //user comfort
                var idGenerator = eid == 0 ? EntityIDGenerator.Random : EntityIDGenerator.Fix(eid);

                var teleportColumn = (TeleportColumn)Entity.Factory.Create(definition,idGenerator);

                var container = SystemContainer.GetByName(k.es_teleport_column);
                teleportColumn.Parent = container.Eid;

                var name = request.Data.GetOrDefault<string>(k.name);
                if (name.IsNullOrEmpty())
                {
                    var tpAmountOnZone = request.Zone.Units.OfType<TeleportColumn>().Count();
                    name = "tp_zone_" + request.Zone.Id + "_" + (tpAmountOnZone + 1);
                }

                teleportColumn.Name = name;
                teleportColumn.Save();

                request.Zone.UnitService.AddDefaultUnit(teleportColumn,position,"tpc",false);
                teleportColumn.AddToZone(request.Zone,position);

                var result = request.Zone.GetBuildingsDictionaryForCharacter(character);

                Transaction.Current.OnCommited(() =>
                {
                    Logger.Info("");
                    Logger.Info("NEW TELEPORT COLUMN EID:" + teleportColumn.Eid );
                    Logger.Info("");
                    Message.Builder.SetCommand(Commands.ZoneGetBuildings).WithData(result).ToClient(request.Session).Send();
                });
                
                scope.Complete();
            }
        }
    }
}