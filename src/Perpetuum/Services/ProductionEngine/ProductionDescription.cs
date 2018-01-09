using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Items;
using Perpetuum.Log;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionDescription
    {
        private readonly IProductionDataAccess _productionDataAccess;
        public readonly int definition;

        public delegate ProductionDescription Factory(int definition);

        public ProductionDescription(IProductionDataAccess productionDataAccess,int definition)
        {
            _productionDataAccess = productionDataAccess;
            this.definition = definition;
        }

        public List<ProductionComponent> Components
        {
            get { return _productionDataAccess.ProductionComponents.GetOrEmpty(definition).ToList(); }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var replyDict = new Dictionary<string, object>
            {
                {k.definition, definition}
            };

            if (Components.Count == 0)
            {
                Logger.Info("no components for: " + definition);
                return replyDict;
            }

            var counter = 0;
            var componentsDict = Components.ToDictionary<ProductionComponent, string, object>(component => "c" + counter++, component => component.ToDictionary());

            replyDict.Add(k.components, componentsDict);
            return replyDict;
        }

        public int GetPrototypeDefinition()
        {
            return _productionDataAccess.GetPrototypePair(definition);
        }

        public IEnumerable<ProductionLiveComponent> SearchForAvailableComponents(Container container)
        {
            return ProductionHelper.SearchForAvailableComponents(container, Components.ToList());
        }

        public IEnumerable<ProductionLiveComponent> ProcessComponentRequirement(ProductionInProgressType productionType, List<ProductionLiveComponent> foundComponents, int targetAmount, double materialMultiplier)
        {
            return ProductionHelper.ProcessComponentRequirement(productionType, foundComponents, targetAmount, materialMultiplier, Components);
        }

        public static Dictionary<string, object> GetRequiredComponentsInfo(ProductionInProgressType productionInProgressType, int targetAmount, double materialMultiplier, List<ProductionComponent> components)
        {
            var result = new Dictionary<string, object>();
            var counter = 0;

            foreach (var component in components)
            {
                if (component.IsSkipped(productionInProgressType)) continue;

                var oneComponent = new Dictionary<string, object>
                {
                    {k.definition, component.EntityDefault.Definition}
                };

                //single component
                if (component.IsSingle)
                {
                    oneComponent.Add(k.amount, targetAmount);
                    oneComponent.Add(k.effectiveAmount, 1);
                }
                else
                {
                    oneComponent.Add(k.effectiveAmount, component.EffectiveAmount(targetAmount, materialMultiplier));
                    oneComponent.Add(k.nominalAmount, component.Amount);
                }

                result.Add("c" + counter++, oneComponent);
            }

            return result;
        }

        public static ErrorCodes UpdateUsedComponents(IEnumerable<ProductionLiveComponent> usedComponents, Container container, Character character, TransactionType transactionType)
        {
            var ec = ErrorCodes.NoError;

            foreach (var component in usedComponents)
            {
                var componentItem = container.GetItem(component.eid, true);
                if (componentItem == null)
                {
                    ec = ErrorCodes.ItemNotFound;
                    return ec;
                }

                var logQuantity = componentItem.Quantity;

                if (component.resultQuantity == 0)
                {
                    Entity.Repository.Delete(componentItem);
                }
                else
                {
                    logQuantity = componentItem.Quantity - component.resultQuantity;
                    componentItem.Quantity = component.resultQuantity;
                }

                var b = TransactionLogEvent.Builder().SetTransactionType(transactionType).SetCharacter(character).SetItem(componentItem.Definition, logQuantity);
                character.LogTransaction(b);
            }

            return ec;
        }

        public Item CreateRefineResult(long owner, Container container, int quantity, Character character)
        {
            var resultItem = (Item) Entity.Factory.CreateWithRandomEID(definition);
            resultItem.Owner = owner;
            resultItem.Quantity = quantity;

            container.AddItem(resultItem, true);
            character.WriteItemTransactionLog(TransactionType.RefineCreated, resultItem);
            return resultItem;
        }

        /// <summary>
        /// ONLY ADMIN
        /// </summary>
        public void SpawnRequiredComponentsAdmin(Character character)
        {
            var container = character.GetPublicContainer();

            foreach (var component in Components)
            {
                //skip license, since that's only possible to get by creating it from the patent
                if (EntityDefault.Get(component.EntityDefault.Definition).CategoryFlags.IsCategory(CategoryFlags.cf_documents))
                    continue;

                var entity = Entity.Factory.CreateWithRandomEID(component.EntityDefault);
                entity.Owner = character.Eid;
                entity.Parent = container.Eid;
                entity.Quantity = (int) (component.Amount * (1 + (FastRandom.NextDouble() * 2)));
                entity.Save();
            }
        }

        public void ScaleComponents(double scale, CategoryFlags componentCategoryFlag)
        {
            Logger.Info("scaling description: " + definition + " " + EntityDefault.Get(definition).Name);

            foreach (var component in Components)
            {
                //if not the defined category for example raw material
                var tmpFlag = EntityDefault.Get(component.EntityDefault.Definition).CategoryFlags;

                if ((tmpFlag & componentCategoryFlag.GetCategoryFlagsMask()) != componentCategoryFlag)
                {
                    Logger.Info("component skipped: " + component.EntityDefault.Definition + " " + EntityDefault.Get(component.EntityDefault.Definition).Name);
                    continue;
                }

                Db.Query().CommandText("update components set componentamount=@camount where definition=@definition and componentdefinition=@cdefinition")
                    .SetParameter("@camount", (int) (component.Amount * scale))
                    .SetParameter("@cdefinition", component.EntityDefault.Definition)
                    .SetParameter("@definition", definition)
                    .ExecuteNonQuery();

                Logger.Info("component " + component.EntityDefault.Definition + " scaled from: " + component.Amount + " to " + (int) (component.Amount * scale));
            }
        }


        public override string ToString()
        {
            var result = "def:" + definition;

            foreach (var productionComponent in Components)
            {
                result += " comp:" + productionComponent.EntityDefault.Definition + " amount:" + productionComponent.Amount;
            }

            return result;
        }
    }
}
