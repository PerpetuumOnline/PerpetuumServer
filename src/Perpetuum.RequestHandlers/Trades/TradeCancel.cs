using Perpetuum.Host.Requests;
using Perpetuum.Services.Trading;

namespace Perpetuum.RequestHandlers.Trades
{
    public class TradeCancel : TradeRequestHandler
    {
        private readonly ITradeService _tradeService;

        public TradeCancel(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }


        public override void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            _tradeService.ClearTrade(character);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}