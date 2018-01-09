using System.Collections.Generic;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations.YellowPages
{
    public class YellowPagesGet : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public YellowPagesGet(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var corporationeid = character.CorporationEid;

            DefaultCorporationDataCache.IsCorporationDefault(corporationeid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);
            var entry = _corporationManager.GetYellowPages(corporationeid);
            var result = new Dictionary<string, object> { { k.data, entry } };
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}