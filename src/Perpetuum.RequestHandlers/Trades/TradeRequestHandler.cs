using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Trades
{
    public abstract class TradeRequestHandler : IRequestHandler
    {

        protected static void CheckTradersAndThrowIfFailed(Character character, Character trader)
        {
            character.ThrowIfEqual(trader, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
            character.CheckPrivilegedTransactionsAndThrowIfFailed();

            trader.BlockTrades.ThrowIfTrue(ErrorCodes.TradeIsBlocked);
            trader.IsOnline.ThrowIfFalse(ErrorCodes.TraderIsOffline);
            trader.IsBlocked(character).ThrowIfTrue(ErrorCodes.TargetBlockedTheRequest);
            trader.IsDocked.ThrowIfFalse(ErrorCodes.TraderHasToBeDocked);
            trader.CheckPrivilegedTransactionsAndThrowIfFailed();

            var myDockingBase = character.GetCurrentDockingBase();
            var hisDockingBase = trader.GetCurrentDockingBase();
            myDockingBase.Eid.ThrowIfNotEqual(hisDockingBase.Eid, ErrorCodes.DockingBaseMismatch);
        }

        public abstract void HandleRequest(IRequest request);
    }
}