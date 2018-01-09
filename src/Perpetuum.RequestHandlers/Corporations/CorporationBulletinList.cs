using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationBulletinList : IRequestHandler
    {
        private readonly IBulletinHandler _bulletinHandler;

        public CorporationBulletinList(IBulletinHandler bulletinHandler)
        {
            _bulletinHandler = bulletinHandler;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporation = character.GetPrivateCorporationOrThrow();
            Message.Builder.FromRequest(request).WithData(_bulletinHandler.GetBulletinList(corporation.Eid)).Send();
        }
    }
}