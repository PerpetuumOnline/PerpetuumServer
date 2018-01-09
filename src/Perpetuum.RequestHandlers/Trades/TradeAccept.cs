using System;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Trading;

namespace Perpetuum.RequestHandlers.Trades
{
    public class TradeAccept : TradeRequestHandler
    {
        private readonly ITradeService _tradeService;

        public TradeAccept(ITradeService tradeService)
        {
            _tradeService = tradeService;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                try
                {
                    var myTrade = _tradeService.GetTrade(character).ThrowIfNull(ErrorCodes.TradeNotFound);

                    CheckTradersAndThrowIfFailed(character, myTrade.trader);

                    lock (myTrade.commonSync)
                    {
                        myTrade.State = TradeState.Accept;

                        var hisTrade = _tradeService.GetTrade(myTrade.trader).ThrowIfNull(ErrorCodes.TradeNotFound);

                        if (hisTrade.State == TradeState.Accept)
                        {
                            try
                            {
                                var myContainer = character.GetPublicContainerWithItems();
                                var hisContainer = hisTrade.owner.GetPublicContainerWithItems();

                                myTrade.TransferItems(hisTrade, myContainer, hisContainer);
                                hisTrade.TransferItems(myTrade, hisContainer, myContainer);

                                myContainer.Save();
                                hisContainer.Save();

                                myTrade.SendFinishCommand(myContainer);
                                hisTrade.SendFinishCommand(hisContainer);
                            }
                            catch (Exception)
                            {
                                myTrade.State = TradeState.Offer;
                                hisTrade.State = TradeState.Offer;
                                throw;
                            }
                        }
                    }

                    Message.Builder.FromRequest(request).WithOk().Send();
                }
                catch (Exception)
                {
                    _tradeService.ClearTrade(character);
                    throw;
                }
                
                scope.Complete();
            }
        }
    }
}