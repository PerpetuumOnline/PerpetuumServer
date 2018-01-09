using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Trading
{
    public interface ITradeService
    {
        [CanBeNull]
        Trade GetTrade(Character character);
        void AddTrade(Character character, Trade trade);
        void ClearTrade(Character character);
    }
}