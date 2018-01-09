using System;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Trading;

namespace Perpetuum.RequestHandlers.Trades
{
    public class TradeSetOffer : TradeRequestHandler
    {
        private readonly ITradeService _tradeService;

        public TradeSetOffer(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        public override void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var credit = request.Data.GetOrDefault<long>(k.credit);
            var items = request.Data.GetOrDefault<long[]>(k.items);

            try
            {
                var myTrade = _tradeService.GetTrade(character).ThrowIfNull(ErrorCodes.TradeNotFound);

                CheckTradersAndThrowIfFailed(character, myTrade.trader);

                var hisTrade = _tradeService.GetTrade(myTrade.trader).ThrowIfNull(ErrorCodes.TradeNotFound);

                lock (myTrade.commonSync)
                {
                    myTrade.SetOffer(credit, items);
                    hisTrade.State = TradeState.Offer;
                }

                Message.Builder.FromRequest(request).WithOk().Send();
            }
            catch (Exception)
            {
                _tradeService.ClearTrade(character);
                throw;
            }
        }
    }
}