using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketSetState : MarketStateRequestHandler
    {
        private readonly IMarketRobotPriceWriter _robotPriceWriter;

        public MarketSetState(IMarketRobotPriceWriter robotPriceWriter,IMarketInfoService marketInfoService) : base(marketInfoService)
        {
            _robotPriceWriter = robotPriceWriter;
        }

        public override void HandleRequest(IRequest request)
        {
            _robotPriceWriter.WriteRobotPrices();
            var result = GetMarketState();
            Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
        }
    }
}