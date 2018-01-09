using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Robots;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionComponentCollector : IEntityVisitor<Item>, IEntityVisitor<Robot>
    {
        private readonly IProductionDataAccess _productionDataAccess;
        private readonly List<ProductionComponent> _components = new List<ProductionComponent>();

        public delegate ProductionComponentCollector Factory();

        public static Factory ProductionComponentCollectorFactory { get; set; }

        public ProductionComponentCollector(IProductionDataAccess productionDataAccess)
        {
            _productionDataAccess = productionDataAccess;
        }

        public static List<ProductionComponent> Collect(Item item)
        {
            var collector = ProductionComponentCollectorFactory();
            item.AcceptVisitor(collector);
            return collector._components;
        }

        private void CollectProductionComponents(Item item)
        {
            _components.AddRange(_productionDataAccess.ProductionComponents[item.Definition]);
        }

        public void Visit(Item item)
        {
            CollectProductionComponents(item);
        }

        public void Visit(Robot robot)
        {
            if (!robot.IsRepackaged)
            {
                robot.VisitRobotComponents(this);
                return;
            }

            CollectProductionComponents(robot);
        }
    }
}