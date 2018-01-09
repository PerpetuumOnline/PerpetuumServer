using System;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Trading;

namespace Perpetuum.RequestHandlers.Trades
{
    public class TradeBegin : TradeRequestHandler
    {
        private readonly ITradeService _tradeService;
        private readonly Trade.Factory _tradeFactory;

        public TradeBegin(ITradeService tradeService,Trade.Factory tradeFactory)
        {
            _tradeService = tradeService;
            _tradeFactory = tradeFactory;
        }

        public override void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var trader = Character.Get(request.Data.GetOrDefault<int>(k.traderID)).ThrowIfNull(ErrorCodes.CharacterNotFound);

            try
            {
                _tradeService.ClearTrade(character);

                if (character == trader)
                    return;

                CheckTradersAndThrowIfFailed(character, trader);

                _tradeService.GetTrade(trader).ThrowIfNotNull(ErrorCodes.TraderIsBusy);

                var commonSync = new object();

                var myTrade = _tradeFactory(character, trader, commonSync);
                _tradeService.AddTrade(character, myTrade);

                var hisTrade = _tradeFactory(trader, character, commonSync);
                _tradeService.AddTrade(trader, hisTrade);

                myTrade.State = TradeState.Begin;
                hisTrade.State = TradeState.Begin;

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