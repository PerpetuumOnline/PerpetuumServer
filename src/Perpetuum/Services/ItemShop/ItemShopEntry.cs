using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.Items;

namespace Perpetuum.Services.ItemShop
{
    public class ItemShopEntry
    {
        private readonly int _id;
        private readonly EntityDefault _targetItemED;
        private readonly int _targetItemAmount;
        private readonly int _tmcoin;
        private readonly int _icscoin;
        private readonly int _asicoin;
        private readonly double _credit;
        private readonly int? _globalLimit;
        private readonly int _purchaseCount;
        private readonly double? _standing;

        public ItemShopEntry(int id,EntityDefault targetItemED,int targetItemAmount,int tmcoin, int icscoin, int asicoin, double credit, int? globalLimit, int purchaseCount,double? standing)
        {
            _id = id;
            _targetItemED = targetItemED;
            _targetItemAmount = targetItemAmount;
            _tmcoin = tmcoin;
            _icscoin = icscoin;
            _asicoin = asicoin;
            _credit = credit;
            _purchaseCount = purchaseCount;
            _globalLimit = globalLimit;
            _standing = standing;
        }

        public int? GlobalLimit
        {
            get { return _globalLimit; }
        }

        public int PurchaseCount
        {
            get { return _purchaseCount; }
        }

        public EntityDefault TargetItemED
        {
            get { return _targetItemED; }
        }

        public int TmCoin
        {
            get { return _tmcoin; }
        }


        public int AsiCoin
        {
            get { return _asicoin; }
        }


        public int IcsCoin
        {
            get { return _icscoin; }
        }


        public double Credit
        {
            get { return _credit; }
        }

        public double? Standing
        {
            get { return _standing; }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string,object>();

            d[k.ID] = _id;
            d[k.targetDefinition] = _targetItemED.Definition;
            d[k.targetAmount] = _targetItemAmount;
            d[k.globalLimit] = _globalLimit;
            d[k.purchaseCount] = _purchaseCount;
            d[k.credit] = _credit;
            d[k.standing] = _standing;
            d[k.tmcoin] = _tmcoin;
            d[k.icscoin] = _icscoin;
            d[k.asicoin] = _asicoin;
            return d;
        }

        public Item CreateTargetItem(Character owner,int quantity)
        {
            var targetItem = (Item)Entity.Factory.CreateWithRandomEID(TargetItemED);
            targetItem.Owner = owner.Eid;
            targetItem.Quantity = _targetItemAmount * quantity;
            return targetItem;
        }

        public void CheckGlobalLimit(int quantity)
        {
            //do global limitation
            if (GlobalLimit == null)
                return;

            if (GlobalLimit <= PurchaseCount)
                throw new PerpetuumException(ErrorCodes.OutOfItemGlobally);

            var availableAmount = (int) GlobalLimit - PurchaseCount;

            //clamp to the available maximum
            if (availableAmount < quantity)
                throw new PerpetuumException(ErrorCodes.ThisAmountIsNotAvailable);
        }

        public void RemoveFromContainer(Container container, int quantity)
        {

            if (_tmcoin > 0)
            {
                var tc = Coin.CreateTMCoin(_tmcoin);
                tc.RemoveFromContainer(container, quantity);
            }

            if (_icscoin > 0)
            {
                var ic = Coin.CreateICSCoin(_icscoin);
                ic.RemoveFromContainer(container, quantity);
            }

            if (_asicoin > 0)
            {
                var ac = Coin.CreateASICoin(_asicoin);
                ac.RemoveFromContainer(container, quantity);
            }
        }
    }
}