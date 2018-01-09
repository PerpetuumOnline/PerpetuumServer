using Perpetuum.Host.Requests;
using Perpetuum.Services.Trading;

namespace Perpetuum.RequestHandlers.Trades
{
    public class TradeRetractOffer : TradeRequestHandler
    {
        private readonly ITradeService _tradeService;

        public TradeRetractOffer(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        public override void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            var myTrade = _tradeService.GetTrade(character).ThrowIfNull(ErrorCodes.TradeNotFound);
            var hisTrade = _tradeService.GetTrade(myTrade.trader).ThrowIfNull(ErrorCodes.TradeNotFound);

            lock (myTrade.commonSync)
            {
                myTrade.State = TradeState.Begin;
                hisTrade.State = TradeState.Offer;
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }


}