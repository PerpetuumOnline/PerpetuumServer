using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;

namespace Perpetuum.RequestHandlers.Zone
{
    public class KioskInfo : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var character = request.Session.Character;
            var eid = request.Data.GetOrDefault<long>(k.eid);

            var guidStr = request.Data.GetOrDefault<string>(k.guid);

            if (!Guid.TryParse(guidStr, out Guid guid))
            {
                Logger.Error("Guid parse error. " + guidStr + " " + request.Command);
                throw new PerpetuumException(ErrorCodes.SyntaxError);
            }

            var kiosk = request.Zone.GetUnitOrThrow<Kiosk>(eid);
            var player = request.Zone.GetPlayerOrThrow(character);
            kiosk.IsInRangeOf3D(player, DistanceConstants.KIOSK_USE_DISTANCE).ThrowIfFalse(ErrorCodes.ItemOutOfRange);

            request.Zone.CreateBeam(BeamType.loot_bolt, b => b.WithSource(player)
                .WithTarget(kiosk)
                .WithState(BeamState.Hit)
                .WithDuration(1000));

            var result = kiosk.GetKioskInfo(player, guid);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}