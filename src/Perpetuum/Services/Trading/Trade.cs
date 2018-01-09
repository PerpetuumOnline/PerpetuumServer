using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;

namespace Perpetuum.Services.Trading
{
    public class Trade
    {
        public readonly Character owner;
        public readonly Character trader;

        private TradeState _state = TradeState.Undefined;

        private long _credit;
        private readonly List<TradeItem> _items = new List<TradeItem>();

        public readonly object commonSync;
        private readonly CharacterWalletFactory _characterWalletFactory;

        public delegate Trade Factory(Character owner, Character trader, object commonSync);

        public Trade(Character owner, Character trader, object commonSync,CharacterWalletFactory characterWalletFactory)
        {
            this.owner = owner;
            this.trader = trader;
            this.commonSync = commonSync;
            _characterWalletFactory = characterWalletFactory;
        }

        private double Credit
        {
            get { return _credit; }
        }

        private IList<TradeItem> GetItems()
        {
            return _items;
        }

        public TradeState State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                    return;

                _state = value;

                var data = new Dictionary<string, object>
                    {
                        {k.state, (int) _state},
                        {k.characterID, owner.Id},
                        {k.traderID, trader.Id}
                    };

                Message.Builder.SetCommand(Commands.TradeState).WithData(data).ToCharacters(owner, trader).Send();
            }
        }

        public void SetOffer(long newCredit, IList<long> newItemEids)
        {
            State = TradeState.Offer;
            _credit = newCredit;

            _items.Clear();

            var data = new Dictionary<string, object>
                           {
                               {k.characterID, owner.Id},
                               {k.credit, _credit},
                           };

            if (!newItemEids.IsNullOrEmpty())
            {
                var myContainer = owner.GetPublicContainerWithItems();

                foreach (var newItemEid in newItemEids)
                {
                    var item = myContainer.GetItem(newItemEid);
                    if (item == null)
                        continue;
                    _items.Add(new TradeItem(item));
                }

                var items = _items.ToDictionary<TradeItem, string, object>(item => "i" + item.itemEid, item => item.ToDictionary());
                data.Add(k.items, items);
            }

            Message.Builder.SetCommand(Commands.TradeOffer).WithData(data).ToCharacters(owner, trader).Send();
        }

        public void SendFinishCommand(Container container = null)
        {
            var data = new Dictionary<string, object>();

            if (container != null)
            {
                data.Add(k.container, container.ToDictionary());
            }

            Message.Builder.SetCommand(Commands.TradeFinished).WithData(data).ToCharacter(owner).Send();
        }

        public void TransferItems(Trade hisTrade, Container sourceContainer, Container targetContainer)
        {
            var tradeItems = GetItems();

            foreach (var tradeItem in tradeItems)
            {
                var item = sourceContainer.GetItemOrThrow(tradeItem.itemEid);
                item.Quantity.ThrowIfNotEqual(tradeItem.ItemInfo.Quantity, ErrorCodes.ItemQuantityMismatch);
            }

            sourceContainer.RelocateItems(owner, hisTrade.owner, tradeItems.Select(i => i.itemEid), targetContainer);

            var outEBuilder = TransactionLogEvent.Builder().SetTransactionType(TransactionType.TradeItemOut).SetCharacter(owner).SetInvolvedCharacter(hisTrade.owner);
            var inEBuilder = TransactionLogEvent.Builder().SetTransactionType(TransactionType.TradeItemIn).SetCharacter(hisTrade.owner).SetInvolvedCharacter(owner);

            foreach (var tradeItem in tradeItems)
            {
                outEBuilder.SetItem(tradeItem.ItemInfo.Definition, tradeItem.ItemInfo.Quantity);
                owner.LogTransaction(outEBuilder);

                inEBuilder.SetItem(tradeItem.ItemInfo.Definition, tradeItem.ItemInfo.Quantity);
                hisTrade.owner.LogTransaction(inEBuilder);
            }

            TransferCredit();
        }

        private void TransferCredit()
        {
            if (Credit <= 0.0)
                return;

            var ownerWallet = _characterWalletFactory(owner, TransactionType.TradeSpent);
            ownerWallet.Balance -= Credit;
            owner.LogTransaction(TransactionLogEvent.Builder()
                                                        .SetTransactionType(TransactionType.TradeSpent)
                                                        .SetCreditBalance(ownerWallet.Balance)
                                                        .SetCreditChange(-Credit)
                                                        .SetCharacter(owner)
                                                        .SetInvolvedCharacter(trader).Build());

            var traderWallet = _characterWalletFactory(trader, TransactionType.TradeGained);
            traderWallet.Balance += Credit;
            trader.LogTransaction(TransactionLogEvent.Builder()
                                                        .SetTransactionType(TransactionType.TradeGained)
                                                        .SetCreditBalance(traderWallet.Balance)
                                                        .SetCreditChange(Credit)
                                                        .SetCharacter(trader)
                                                        .SetInvolvedCharacter(owner).Build());
        }
    }

}
