using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionComponent
    {
        public EntityDefault EntityDefault { get; private set; }
        public int Amount { get; private set; }

        public ProductionComponent(EntityDefault entityDefault,int amount)
        {
            EntityDefault = entityDefault;
            Amount = amount;
        }

        public bool IsMaterial
        {
            get { return EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_material); }
        }

        public bool IsSingle
        {
            get { return Amount == 1; }
        }

        public bool IsRobotShard
        {
            get { return EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_robotshards); }
        }

        public bool IsEquipment
        {
            get { return EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_robot_equipment); }
        }

        public bool IsRobot
        {
            get { return EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_robots); }
        }

        public bool IsReactorCore
        {
            get { return EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_reactor_cores); }
        }

        public int EffectiveAmount(int targetAmount, double materialMultiplier)
        {
            //1 or higher :D
            return IsSingle ? targetAmount : Math.Max(1, (int)Math.Round((targetAmount * Amount * materialMultiplier)));
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.definition,EntityDefault.Definition},
                {k.amount, Amount}
            };
        }

        public bool IsSkipped(ProductionInProgressType productionInProgressType)
        {
            switch (productionInProgressType)
            {
                case ProductionInProgressType.insurance:
                case ProductionInProgressType.reprocess:
                case ProductionInProgressType.refine:
                    return IsSingle || IsRobotShard;

                case ProductionInProgressType.massProduction:
                    return IsRobotShard;
            }
            
            return false;
        }
    }

    public class ProductionLiveComponent
    {
        public int definition;
        public long eid;
        public int quantity;
        public int resultQuantity;
    }
}
