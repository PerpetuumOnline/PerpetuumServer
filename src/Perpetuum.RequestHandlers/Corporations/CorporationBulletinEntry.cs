using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationBulletinEntry : IRequestHandler
    {
        private readonly IBulletinHandler _bulletinHandler;

        public CorporationBulletinEntry(IBulletinHandler bulletinHandler)
        {
            _bulletinHandler = bulletinHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var entry = request.Data.GetOrDefault<string>(k.text);
                var bulletinID = request.Data.GetOrDefault<int>(k.bulletinID);

                string.IsNullOrEmpty(entry).ThrowIfTrue(ErrorCodes.TextEmpty);

                var corporation = character.GetPrivateCorporationOrThrow();
                _bulletinHandler.BulletinExists(bulletinID, corporation.Eid).ThrowIfFalse(ErrorCodes.ItemNotFound);

                var id = _bulletinHandler.InsertEntry(bulletinID, character.Id, entry);

                var result = new Dictionary<string, object>(4)
                {
                    { k.bulletinID, bulletinID },
                    { k.entryID, id },
                    { k.text, entry },
                    { k.characterID, character.Id },
                    { k.date, DateTime.Now }
                };

                Message.Builder.SetCommand(request.Command)
                    .WithData(result)
                    .ToCharacters(corporation.GetCharacterMembers()).Send();

                var bulletinDescription = _bulletinHandler.GetBulletin(bulletinID);
                Transaction.Current.OnCommited(() => _bulletinHandler.SendBulletinUpdate(bulletinDescription, CorporationBulletinEvent.newEntry, character));
                
                scope.Complete();
            }
        }
    }
}