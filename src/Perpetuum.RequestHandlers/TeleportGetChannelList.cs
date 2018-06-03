using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Zones;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.RequestHandlers
{
    public class TeleportGetChannelList : IRequestHandler<IZoneRequest>
    {
        private readonly IZoneManager _zoneManager;

        public TeleportGetChannelList(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var teleportEid = request.Data.GetOrDefault<long>(k.eid);
            var teleport = _zoneManager.GetUnit<Teleport>(teleportEid);
            if (teleport == null)
                throw new PerpetuumException(ErrorCodes.TeleportNotFound);
            
            //Check if mobile -- Throw if character is not owner, or in owner's gang
            if (teleport is MobileTeleport mobile)
            {
                var character = request.Session.Character;
                var ownerCharacter = mobile.GetOwnerAsCharacter();
                if (character != ownerCharacter)
                {
                    var playerGang = character.GetGang();
                    playerGang.ThrowIfNull(ErrorCodes.CharacterNotInGang);
                    playerGang.IsMember(ownerCharacter).ThrowIfFalse(ErrorCodes.CharacterNotInTheOwnerGang);
                }
            }

            var result = teleport.ToDictionary();
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}