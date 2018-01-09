using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ItemShop
{
    public class Coin
    {
        private readonly EntityDefault _coinED;
        private readonly int _amount;

        private static readonly Dictionary<EntityDefault,string> _coinKeyNames = new Dictionary<EntityDefault, string>()
        {
            {EntityDefault.GetByName(DefinitionNames.TM_MISSION_COIN),k.tmcoin},
            {EntityDefault.GetByName(DefinitionNames.ICS_MISSION_COIN),k.icscoin},
            {EntityDefault.GetByName(DefinitionNames.ASI_MISSION_COIN),k.asicoin},
            {EntityDefault.GetByName(DefinitionNames.UNIVERSAL_MISSION_COIN),k.unicoin}
        };

        private Coin(EntityDefault coinED,int amount)
        {
            _coinED = coinED;
            _amount = amount;
        }

        public EntityDefault CoinED
        {
            get { return _coinED; }
        }

        public int Amount
        {
            get { return _amount; }
        }

        public void AddInfoToDictionary(Dictionary<string, object> dictionary)
        {
            dictionary[_coinKeyNames[_coinED]] = _amount;
        }

        public void RemoveFromContainer(Container container,int quantity)
        {
            var requestedQuantity = Amount * quantity;
            var qty = container.RemoveItemByDefinition(CoinED.Definition, requestedQuantity);
            if (qty < requestedQuantity)
                throw new PerpetuumException(ErrorCodes.NotEnoughCoins);
        }

        public static Coin CreateTMCoin(int amount)
        {
            return new Coin(EntityDefault.GetByName(DefinitionNames.TM_MISSION_COIN),amount);
        }

        public static Coin CreateICSCoin(int amount)
        {
            return new Coin(EntityDefault.GetByName(DefinitionNames.ICS_MISSION_COIN),amount);
        }

        public static Coin CreateASICoin(int amount)
        {
            return new Coin(EntityDefault.GetByName(DefinitionNames.ASI_MISSION_COIN),amount);
        }

        public static Coin CreateUniversalCoin(int amount)
        {
            return new Coin(EntityDefault.GetByName(DefinitionNames.UNIVERSAL_MISSION_COIN),amount);
        }

    }
}