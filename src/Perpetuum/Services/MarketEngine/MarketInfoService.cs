
namespace Perpetuum.Services.MarketEngine
{
    public class MarketInfoService : IMarketInfoService
    {
        private readonly double _margin;
        private readonly double _fee;
        private readonly bool _checkAveragePrice;

        public const int MARKET_AVERAGE_DAYSBACK = 14;
        public const int MARKET_AVERAGE_EXPIRY_MINUTES = 60;
        public const int MARKET_CANCEL_TIME = 10; //minutes

        public MarketInfoService(double margin,double fee,bool checkAveragePrice)
        {
            _margin = margin;
            _fee = fee;
            _checkAveragePrice = checkAveragePrice;
        }

        public double Fee
        {
            get { return _fee; }
        }

        public double Margin
        {
            get { return _margin; }
        }

        public bool CheckAveragePrice
        {
            get { return _checkAveragePrice; }
        }
    }
}