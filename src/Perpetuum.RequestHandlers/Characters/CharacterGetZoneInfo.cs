using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{

    /// <summary>
    /// Retuns the sender's zoneId
    /// </summary>
    public class CharacterGetZoneInfo : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
            var zoneID = character.ZoneId.ThrowIfNull(ErrorCodes.CharacterHasToBeUnDocked);

            var dictionary = new Dictionary<string, object>
            {
                {k.zoneID, (int)zoneID},
                {k.characterID, character.Id}
            };

            Message.Builder.FromRequest(request).WithData(dictionary).Send();
        }
    }
}