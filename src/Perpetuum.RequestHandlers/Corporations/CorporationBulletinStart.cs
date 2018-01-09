using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationBulletinStart : IRequestHandler
    {
        private readonly IBulletinHandler _bulletinHandler;

        public CorporationBulletinStart(IBulletinHandler bulletinHandler)
        {
            _bulletinHandler = bulletinHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var title = request.Data.GetOrDefault<string>(k.title);
                var entryText = request.Data.GetOrDefault<string>(k.text);

                string.IsNullOrEmpty(entryText).ThrowIfTrue(ErrorCodes.TextEmpty);
                string.IsNullOrEmpty(title).ThrowIfTrue(ErrorCodes.TextEmpty);

                var corporation = character.GetPrivateCorporationOrThrow();

                corporation.GetMemberRole(character).IsAnyRole(CorporationRole.CEO, CorporationRole.PRManager, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                //create a bulletin
                var bulletin = _bulletinHandler.StartBulletin(corporation.Eid, title, character);

                //add initial entry
                _bulletinHandler.InsertEntry(bulletin.bulletinID, character.Id, entryText);

                var entries = _bulletinHandler.GetBulletinEntries(bulletin.bulletinID);

                var result = new Dictionary<string, object>(2)
                {
                    { k.details, bulletin.ToDictionary() },
                    { k.entries, entries }
                };

                Message.Builder.FromRequest(request)
                    .WithData(result)
                    .Send();

                Transaction.Current.OnCommited(() => _bulletinHandler.SendBulletinUpdate(bulletin, CorporationBulletinEvent.bulletinStarted, character));
                
                scope.Complete();
            }
        }
    }
}