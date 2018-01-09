using Perpetuum.Threading.Process;

namespace Perpetuum.Services.MarketEngine
{
    public interface IMarketRobotPriceWriter : IProcess
    {
        void WriteRobotPrices();
    }
}