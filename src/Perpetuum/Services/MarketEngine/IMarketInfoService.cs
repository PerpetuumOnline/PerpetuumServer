namespace Perpetuum.Services.MarketEngine
{
    public interface IMarketInfoService
    {
        double Margin { get; }
        double Fee { get; }
        bool CheckAveragePrice { get; }
    }
}