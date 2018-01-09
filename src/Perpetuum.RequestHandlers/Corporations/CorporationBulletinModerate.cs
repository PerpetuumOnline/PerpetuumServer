using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationBulletinModerate : IRequestHandler
    {
        private readonly IBulletinHandler _bulletinHandler;

        public CorporationBulletinModerate(IBulletinHandler bulletinHandler)
        {
            _bulletinHandler = bulletinHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var bulletinID = request.Data.GetOrDefault<int>(k.bulletinID);
                var entryID = request.Data.GetOrDefault<int>(k.ID);
                var entryText = request.Data.GetOrDefault<string>(k.text);

                string.IsNullOrEmpty(entryText).ThrowIfTrue(ErrorCodes.TextEmpty);

                var corporation = character.GetPrivateCorporationOrThrow();

                if (_bulletinHandler.GetEntryOwner(bulletinID, entryID) != character.Id)
                {
                    corporation.GetMemberRole(character).IsAnyRole(CorporationRole.CEO, CorporationRole.PRManager, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
                }

                _bulletinHandler.UpdateEntry(bulletinID, entryID, entryText);

                var result = new Dictionary<string, object>
                {
                    { k.bulletinID, bulletinID },
                    { k.text, entryText },
                    { k.characterID, character.Id },
                    { k.ID, entryID }
                };

                Message.Builder.SetCommand(request.Command)
                    .WithData(result)
                    .ToCharacters(corporation.GetCharacterMembers())
                    .Send();
                
                scope.Complete();
            }
        }
    }
}