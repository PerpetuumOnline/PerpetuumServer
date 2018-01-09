using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketTaxChange : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var marketEid = request.Data.GetOrDefault<long>(k.marketEID);
                var market = Market.GetOrThrow(marketEid);
                var newTax = request.Data.GetOrDefault<double>(k.tax);

                var character = request.Session.Character;
                market.SetTax(character,newTax);
            
                var data = new Dictionary<string, object>
                {
                    {k.market, market.ToDictionary()}
                };

                Message.Builder.FromRequest(request).WithData(data).Send();
                
                scope.Complete();
            }
        }
    }
}