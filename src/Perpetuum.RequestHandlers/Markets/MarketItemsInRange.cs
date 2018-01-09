using System.Collections.Generic;
using System.Linq;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketItemsInRange : IRequestHandler
    {
        private readonly MarketHandler _marketHandler;
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketItemsInRange(MarketHandler marketHandler,IMarketOrderRepository marketOrderRepository)
        {
            _marketHandler = marketHandler;
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var itemDefinition = request.Data.GetOrDefault<int>(k.definition);

            //training character => no markets in range
            if (character.IsInTraining())
            {
                var trainingResult = new Dictionary<string, object>
                {
                    {k.definition, itemDefinition},
                };

                Message.Builder.FromRequest(request).WithData(trainingResult).Send();
                return;
            }

            long? corporationEid = character.CorporationEid;

            if (DefaultCorporationDataCache.IsCorporationDefault((long)corporationEid))
            {
                corporationEid = null;
            }

            var trainingMarketEid = _marketHandler.GetTrainingMarketEid();

            //raw select by definition
            //exclude training market
            //include corp orders if character in private corp
            //include normal orders (non corp)
            var orders = _marketOrderRepository.GetAllByDefinition(itemDefinition).Where(r => r.marketEID != trainingMarketEid && (r.forMembersOf == corporationEid || r.forMembersOf == null)).ToArray();


            //ez szabalyozza le, hogy nem latszanak a pbs marketek
            //itt lehet ugyeskedni
            //filtered markets
            //var orders = repo.GetByMarketEids(MarketHandler.GetAllDefaultMarketsEids(), itemDefinition, request.character.CorporationEid).ToArray();

            var orderDict = new Dictionary<string, object>(orders.Length);
            var counter = 0;
            foreach (var oneOrder in orders.Select(order => order.ToDictionary()))
            {
                orderDict.Add("m" + counter++, oneOrder);
            }

            var result = new Dictionary<string, object>
            {
                {k.definition, itemDefinition},
                {k.orders, orderDict}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}