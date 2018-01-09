using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.Items
{
    public class ItemPriceHelper
    {
        private readonly Dictionary<int, double> _prices;

        public ItemPriceHelper()
        {
            _prices = LoadPrices();
        }

        private Dictionary<int, double> LoadPrices()
        {
            return Db.Query().CommandText("select * from itemprices")
                          .Execute()
                          .ToDictionary(r => r.GetValue<int>("definition"),r => r.GetValue<double>("price"));
        }

        public double GetDefaultPrice(int definition)
        {
            return _prices.GetOrDefault(definition);
        }
    }

    public class PriceCalculator : IEntityVisitor<Item>,IEntityVisitor<Robot>
    {
        private readonly MarketHandler _marketHandler;
        private readonly IProductionDataAccess _productionDataAccess;
        private readonly IRobotTemplateRelations _robotTemplateRelations;

        public delegate PriceCalculator Factory();

        public static Factory PriceCalculatorFactory { get; set; }

        public PriceCalculator(MarketHandler marketHandler,IProductionDataAccess productionDataAccess,IRobotTemplateRelations robotTemplateRelations)
        {
            _marketHandler = marketHandler;
            _productionDataAccess = productionDataAccess;
            _robotTemplateRelations = robotTemplateRelations;
        }

        private double _price;

        public void Visit(Item item)
        {
            _price += item.Quantity * GetAverageWorldPriceByComponents(item.ED);
        }

        public void Visit(Robot robot)
        {
            robot.VisitRobotComponents(this);
        }

        public static double GetAveragePrice(Item item)
        {
            var calc = PriceCalculatorFactory();
            item.AcceptVisitor(calc);
            return calc._price;
        }

        private double GetAverageWorldPriceByComponents(EntityDefault entityDefault)
        {
            Logger.Info("wavg for " + entityDefault.Name + " " + entityDefault.Definition);

            var sumCost = 0.0;
            foreach (var component in _productionDataAccess.ProductionComponents[entityDefault.Definition])
            {
                if (component.IsRobotShard)
                    continue;

                // COMMODITY es MATERIAL
                if (component.IsMaterial)
                {
                    var commodityComponents = _productionDataAccess.ProductionComponents[component.EntityDefault.Definition].ToArray();
                    if (commodityComponents.Length > 0)
                    {
                        //this is a commodity =>  do components
                        Logger.Info("   commodity break down for: " + component.EntityDefault.Name);

                        foreach (var commodityComponent in commodityComponents)
                        {
                            var marketAverage = _marketHandler.GetWorldAveragePriceByTrades(commodityComponent.EntityDefault);
                            var cost = commodityComponent.Amount * marketAverage * component.Amount;

                            if (marketAverage > 0)
                            {
                                sumCost += cost;
                                Logger.Info("   comm=>raw comp: " + EntityDefault.Get(commodityComponent.EntityDefault.Definition).Name + " q:" + commodityComponent.Amount * component.Amount + " mavg: " + marketAverage + " cost:" + cost);
                            }
                            else
                            {
                                Logger.Info("    comp WTF - no trade - " + EntityDefault.Get(commodityComponent.EntityDefault.Definition).Name);
                            }
                        }
                    }
                    else
                    {
                        // raw material => get market average
                        var marketAverage = _marketHandler.GetWorldAveragePriceByTrades(component.EntityDefault);
                        var cost = component.Amount * marketAverage;

                        if (marketAverage > 0)
                        {
                            sumCost += cost;
                            Logger.Info("    raw comp: " + component.EntityDefault.Name + " q:" + component.Amount + " mavg: " + marketAverage + " cost:" + cost);
                        }
                        else
                        {
                            Logger.Info("    comp WTF - no trade - " + component.EntityDefault.Name);
                        }
                    }
                }

                if (component.IsEquipment)
                {
                    Logger.Info("  component is equipment: " + component.EntityDefault.Name + " recursion starts");

                    var marketAverage = GetAverageWorldPriceByComponents(component.EntityDefault);

                    var cost = component.Amount * marketAverage;

                    if (marketAverage > 0)
                    {
                        sumCost += cost;
                        Logger.Info("   equipment comp: " + component.EntityDefault.Name + " q:" + component.Amount + " mavg: " + marketAverage + " cost:" + cost);
                    }
                    else
                    {
                        Logger.Info("    comp WTF - no trade - " + component.EntityDefault.Name);
                    }
                }

                if (!component.IsRobot)
                    continue;

                Logger.Info("  component is robot: " + component.EntityDefault.Name + " recursion starts");

                var template = _robotTemplateRelations.GetRelatedTemplateOrDefault(component.EntityDefault.Definition);

                var marketAverageHead = GetAverageWorldPriceByComponents(template.Head.EntityDefault);
                var costHead = component.Amount * marketAverageHead;

                if (marketAverageHead > 0)
                {
                    sumCost += costHead;
                    Logger.Info("  head comp: " + template.Head.EntityDefault.Name + " q:" + component.Amount + " mavg: " + marketAverageHead + " cost:" + costHead);
                }
                else
                {
                    Logger.Info("    comp WTF - no trade - " + template.Head.EntityDefault.Name);
                }

                var marketAverageChassis = GetAverageWorldPriceByComponents(template.Chassis.EntityDefault);
                var costChassis = component.Amount * marketAverageChassis;

                if (marketAverageChassis > 0)
                {
                    sumCost += costChassis;
                    Logger.Info("   chassis comp: " + template.Chassis.EntityDefault.Name + " q:" + component.Amount + " mavg: " + marketAverageChassis + " cost:" + costChassis);
                }
                else
                {
                    Logger.Info("    comp WTF - no trade - " + template.Chassis.EntityDefault.Name);
                }

                var marketAverageLeg = GetAverageWorldPriceByComponents(template.Leg.EntityDefault);
                var costLeg = component.Amount * marketAverageLeg;

                if (marketAverageLeg > 0)
                {
                    sumCost += costLeg;
                    Logger.Info("   leg comp: " + template.Leg.EntityDefault.Name + " q:" + component.Amount + " mavg: " + marketAverageLeg + " cost:" + costLeg);
                }
                else
                {
                    Logger.Info("    comp WTF - no trade - " + template.Leg.EntityDefault.Name);
                }
            }

            Logger.Info("total comp sum for " + entityDefault.Name + "  " + sumCost);
            return sumCost;
        }

    }
}