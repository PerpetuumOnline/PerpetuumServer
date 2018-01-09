using System.Collections.Generic;
using System.Linq;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketItemList : IRequestHandler
    {
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketItemList(IMarketOrderRepository marketOrderRepository)
        {
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var itemDefinition = request.Data.GetOrDefault<int>(k.definition);

            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

            var market = character.GetCurrentDockingBase().GetMarketOrThrow();

            var result = new Dictionary<string, object>
            {
                {k.definition, itemDefinition},
                {
                    k.item, _marketOrderRepository.GetByDefinition(itemDefinition, character.CorporationEid, market)
                            .Select(i => i.ToDictionary()).ToDictionary("m", d => d)
                }
            };

            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}