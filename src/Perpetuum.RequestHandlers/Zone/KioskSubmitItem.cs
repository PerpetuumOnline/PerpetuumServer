using System;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class KioskSubmitItem : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var targetKioskEid = request.Data.GetOrDefault<long>(k.target);
                var itemEid = request.Data.GetOrDefault<long>(k.eid);
                var guidStr = request.Data.GetOrDefault<string>(k.guid);

                Guid guid;
                if (!Guid.TryParse(guidStr, out guid))
                {
                    Logger.Error("Guid parse error. " + guidStr + " " + request.Command);
                    throw new PerpetuumException(ErrorCodes.SyntaxError);
                }

                var player = request.Zone.GetPlayerOrThrow(character);
                var kiosk = request.Zone.GetUnit(targetKioskEid).ThrowIfNotType<Kiosk>(ErrorCodes.ItemNotFound);

                kiosk.IsInRangeOf3D(player, DistanceConstants.KIOSK_USE_DISTANCE).ThrowIfFalse(ErrorCodes.ItemOutOfRange);

                var container = player.GetContainer();

                container.EnlistTransaction();

                var itemToSubmit = container.GetItemOrThrow(itemEid);
                var success = player.MissionHandler.SubmitItemToKiosk(kiosk, itemToSubmit, guid);

                container.Save();

                Transaction.Current.OnCommited(() =>
                {
                    if (success)
                        kiosk.CreateSuccessBeam(player);

                    Message.Builder.SetCommand(Commands.ListContainer)
                        .WithData(container.ToDictionary())
                        .ToCharacter(character)
                        .Send();
                });

                scope.Complete();
            }
        }
    }
}