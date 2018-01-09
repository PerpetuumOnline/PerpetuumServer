using Perpetuum.Containers;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;

namespace Perpetuum.Common.Loggers.Transaction
{
    public class TransactionLogEventBuilder
    {
        private TransactionType _transactionType;
        private double _creditBalance;
        private double _creditChange;

        private int _characterID;
        private int _involvedCharacterID;

        private long _corporationEid;
        private long _involvedCorporationEid;

        private long _containerEid;

        private int _itemDefinition;
        private int _itemQuantity;

        public TransactionLogEventBuilder SetTransactionType(TransactionType type)
        {
            _transactionType = type;
            return this;
        }

        public TransactionLogEventBuilder SetCreditBalance(double balance)
        {
            _creditBalance = balance;
            return this;
        }

        public TransactionLogEventBuilder SetCreditChange(double change)
        {
            _creditChange = change;
            return this;
        }

        public TransactionLogEventBuilder SetCharacter(int characterID)
        {
            _characterID = characterID;
            return this;
        }

        public TransactionLogEventBuilder SetInvolvedCharacter(int characterID)
        {
            _involvedCharacterID = characterID;
            return this;
        }

        public TransactionLogEventBuilder SetCorporation(long corporationEid)
        {
            _corporationEid = corporationEid;
            return this;
        }

        public TransactionLogEventBuilder SetInvolvedCorporation(long corporationEid)
        {
            _involvedCorporationEid = corporationEid;
            return this;
        }

        public TransactionLogEventBuilder SetContainer(long containerEid)
        {
            _containerEid = containerEid;
            return this;
        }

        public TransactionLogEventBuilder SetItem(int definition, int quantity)
        {
            _itemDefinition = definition;
            _itemQuantity = quantity;
            return this;
        }

        public TransactionLogEventBuilder SetCorporation(Corporation corporation)
        {
            return SetCorporation(corporation.Eid);
        }

        public TransactionLogEventBuilder SetInvolvedCorporation(Corporation corporation)
        {
            return SetInvolvedCorporation(corporation.Eid);
        }

        public TransactionLogEventBuilder SetContainer(Container container)
        {
            return SetContainer(container.Eid);
        }

        public TransactionLogEventBuilder SetItem(Item item)
        {
            return SetItem(item.Definition,item.Quantity);
        }


        public TransactionLogEvent Build()
        {
            var e = new TransactionLogEvent();
            e.TransactionType = _transactionType;
            e.CreditBalance = _creditBalance;
            e.CreditChange = _creditChange;
            e.CharacterID = _characterID;
            e.InvolvedCharacterID = _involvedCharacterID;
            e.CorporationEid = _corporationEid;
            e.InvolvedCorporationEid = _involvedCorporationEid;
            e.ContainerEid = _containerEid;
            e.ItemDefinition = _itemDefinition;
            e.ItemQuantity = _itemQuantity;
            return e;
        }
    }
}