using System;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationBulletinNewEntries : IRequestHandler
    {
        private readonly IBulletinHandler _bulletinHandler;

        public CorporationBulletinNewEntries(IBulletinHandler bulletinHandler)
        {
            _bulletinHandler = bulletinHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var startTime = request.Data.GetOrDefault<DateTime>(k.time);

            var corporation = character.GetPrivateCorporationOrThrow();
            var result = _bulletinHandler.GetNewBulletinEntries(startTime, corporation.Eid);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}