using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.Insurance;

namespace Perpetuum.Services.ProductionEngine
{
    public class ReprocessSessionMember
    {
        private readonly InsuranceHelper _insuranceHelper;
        private Item _item;
        private bool _wasDeleted;
        private int _resultQuantity;
        private double _healthRatio;
        private Character _character;

        private bool randomizeComponents;

        private readonly List<ProductionItemInfo> _components = new List<ProductionItemInfo>();

        public delegate ReprocessSessionMember Factory();

        public ReprocessSessionMember(InsuranceHelper insuranceHelper)
        {
            _insuranceHelper = insuranceHelper;
        }


        public void Init(Item targetItem, double materialEfficiency, Character character)
        {
            _item = targetItem;
            _character = character;

            //minimal amount check
            if (targetItem.Quantity < targetItem.ED.Quantity)
            {
                return;
            }


            //mission item exception
            if (targetItem.IsCategory(CategoryFlags.cf_mission_items))
            {
                Logger.Info("mission item was found. eid:" + targetItem.Eid);
                randomizeComponents = true;
                _wasDeleted = true;
                return;
            }


            //old version
            //here the item already converted to player
            //0.6 ... 1.0
            //healthRatio = (targetItem.GetHealthRatio() * 0.4)  + 0.6;


            _healthRatio = targetItem.HealthRatio; // 0 ... 1 

            materialEfficiency *= _healthRatio;

            //count how many batches can be made

            var currentQuantity = targetItem.Quantity;
            var defaultQuantity = targetItem.ED.Quantity;

            var leftOver = currentQuantity % defaultQuantity;
            var batchCount = (int)Math.Floor(currentQuantity / (double)defaultQuantity);

            foreach (var component in ProductionComponentCollector.Collect(targetItem))
            {
                if (component.IsSkipped(ProductionInProgressType.reprocess))
                {
                    continue;
                }

                Logger.DebugInfo($"compdef: {component.EntityDefault.Definition} comp amount:{component.Amount} item def:{targetItem.Definition} qty:{targetItem.Quantity} defname:{targetItem.ED.Name}");

                var nominalQuantity = component.Amount * batchCount;

                var newQuantity = (int)(component.Amount * materialEfficiency * batchCount);

                _components.Add(new ProductionItemInfo(component.EntityDefault.Definition, nominalQuantity, newQuantity));

            }


            if (leftOver == 0)
            {
                _wasDeleted = true;

            }
            else
            {
                _resultQuantity = leftOver;

            }
        }


        public void WriteToSql(Container container, Dictionary<int, int> randomComponentResults)
        {
            var logQuantity = _item.Quantity;

            //delete source item
            if (_wasDeleted)
            {
                _insuranceHelper.DeleteAndInform(_item);
                Entity.Repository.Delete(_item);
            }
            else
            {
                logQuantity = _item.Quantity - _resultQuantity;
                _item.Quantity = _resultQuantity;
            }

            _character.LogTransaction(TransactionLogEvent.Builder().SetTransactionType(TransactionType.ReprocessDeleted).SetCharacter(_character).SetItem(_item.Definition, logQuantity));

            if (!randomizeComponents)
            {
                //make the resulting components
                foreach (var component in _components)
                {
                    if (component.realAmount <= 0) 
                        continue;

                    var resultItem = (Item) Entity.Factory.CreateWithRandomEID(component.definition);
                    resultItem.Owner = _item.Owner;
                    resultItem.Quantity = component.realAmount;

                    container.AddItem(resultItem, true);

                    var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.ReprocessCreated).SetCharacter(_character).SetItem(resultItem);
                    _character.LogTransaction(b);
                }
            }
            else
            {
                //pick random components and add them to the container

                Logger.Info("creating random components for " + _item.Eid);

                var configComponents = ProductionComponentCollector.Collect(_item);
                if (configComponents.Count == 0) 
                    return;

                var sumAmount = configComponents.Sum(r => r.Amount);
                var randomPool = new List<int>(sumAmount);

                foreach (var productionComponent in configComponents)
                {
                    for (var i = 0; i < productionComponent.Amount; i++)
                    {
                        randomPool.Add(productionComponent.EntityDefault.Definition);
                    }
                }

                var chosenDefinition = randomPool.ElementAt(FastRandom.NextInt(0, randomPool.Count - 1));

                var chosenComponent = configComponents.First(r => r.EntityDefault.Definition == chosenDefinition);

                var resultItem = (Item) Entity.Factory.CreateWithRandomEID(chosenComponent.EntityDefault.Definition);
                resultItem.Owner = _item.Owner;
                resultItem.Quantity = chosenComponent.Amount*_item.Quantity;

                container.AddItem(resultItem, true);

                _character.WriteItemTransactionLog(TransactionType.ReprocessCreated, resultItem);

                if (randomComponentResults.ContainsKey(resultItem.Definition))
                {
                    randomComponentResults[resultItem.Definition] += resultItem.Quantity;
                }
                else
                {
                    randomComponentResults.Add(resultItem.Definition, resultItem.Quantity);
                }
            }
        }


        public Dictionary<string, object> ToDictionary()
        {

            var counter = 0;
            var oneResult = new Dictionary<string, object>
                                {
                                    {k.eid, _item.Eid}, 
                                    {k.health, _healthRatio}
                                };

            if (!randomizeComponents)
            {
                var componentsDict = _components.ToDictionary<ProductionItemInfo, string, object>(component => "c" + counter++, component => component.ToDictionary());

                oneResult.Add(k.components, componentsDict);
                oneResult.Add(k.unpredictable, 0);
            }
            else
            {
                //itt kell valamit kuldeni a kliensnek h tudja "ebbol random komponensek fognak potyogni" 
                var tibi = new Dictionary<string, object>();
                oneResult.Add(k.components, tibi);
                oneResult.Add(k.unpredictable, 1);
            }

            return oneResult;
        }

    }

}
