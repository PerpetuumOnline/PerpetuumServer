using System.Collections.Concurrent;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.Trading
{
    public class TradeService : ITradeService
    {
        private readonly ConcurrentDictionary<Character,Trade> _trades = new ConcurrentDictionary<Character, Trade>();

        public void ClearTrade(Character character)
        {
            Trade trade;
            if (!_trades.TryRemove(character, out trade)) 
                return;

            lock(trade.commonSync)
            {
                trade.State = TradeState.Cancel;
                trade.SendFinishCommand();

                if (!_trades.TryRemove(trade.trader, out trade)) 
                    return;

                trade.State = TradeState.Cancel;
                trade.SendFinishCommand();
            }
        }

        public void AddTrade(Character character, Trade trade)
        {
            _trades[character] = trade;
        }

        public Trade GetTrade(Character character)
        {
            return _trades.GetOrDefault(character);
        }
    }
}