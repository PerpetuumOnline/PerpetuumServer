using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;

namespace Perpetuum.Services.ProductionEngine.CalibrationPrograms
{
    /// <summary>
    /// Calibration program base that was created by researching an item
    /// </summary>
    public class CalibrationProgram : CalibrationProgramBase
    {
        private readonly IProductionDataAccess _productionDataAccess;

        public CalibrationProgram(IProductionDataAccess productionDataAccess)
        {
            _productionDataAccess = productionDataAccess;
        }

        public override string ToString()
        {
            return "CPRG " + ED.Name + " ";
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void OnInsertToDb()
        {
            MaterialEfficiencyPoints = 50;
            TimeEfficiencyPoints = 50;

            base.OnInsertToDb();
        }

        /// <summary>
        /// Definition from researchlevels
        /// </summary>
        public override int TargetDefinition
        {
            get
            {
                int targetDefinition;

                if (DynamicProperties.Contains(k.target))
                {
                    targetDefinition = DynamicProperties.GetOrDefault<int>(k.target);

                    if (targetDefinition == 0)
                    {
                        SetTargetFromConfig();
                        this.Save();
                        Logger.Info("cprg fixed.  " + this.Eid);
                    }

                    return targetDefinition;

                }

                targetDefinition = SetTargetFromConfig();
                this.Save();
                Logger.Info("cprg initialized.  " + this.Eid);

                return targetDefinition;

            }

        }

        protected int LookUpTargetFromConfig()
        {
            var target = _productionDataAccess.GetResultingDefinitionFromCalibrationDefinition(Definition);
            if (target == 0)
            {
                Logger.Error("no target definition was found for calibration program: " + ED.Name + " " + Definition);
            }

            target = _productionDataAccess.GetOriginalDefinitionFromPrototype(target);
            return target;
        }

        private int SetTargetFromConfig()
        {

            var target = LookUpTargetFromConfig();
            
            DynamicProperties.Set(k.target, target);

            Logger.Info("target definition is set from config. " + this.Eid);
            return target;
        }


        public override Dictionary<string, object> ToDictionary()
        {
            var d = base.ToDictionary();
            d.Add(k.materialEfficiency, MaterialEfficiencyPoints);
            d.Add(k.timeEfficiency, TimeEfficiencyPoints);
            d[k.targetDefinition] = TargetDefinition;
            d[k.targetQuantity] = TargetQuantity;
            return d;
        }

        public override int MaterialEfficiencyPoints
        {
            get { return DynamicProperties.GetOrDefault<int>(k.materialEfficiency); }
            set { DynamicProperties.Set(k.materialEfficiency, value); }
        }

        public override int TimeEfficiencyPoints
        {
            get { return DynamicProperties.GetOrDefault<int>(k.timeEfficiency); }
            set { DynamicProperties.Set(k.timeEfficiency, value); }
        }

        public override List<ProductionComponent> Components => _productionDataAccess.ProductionComponents.GetOrEmpty(TargetDefinition).ToList();

        public override bool HasComponents => _productionDataAccess.ProductionComponents.Contains(TargetDefinition);

        public override bool IsMissionRelated
        {
            get { return false; }
        }

        private double AveragePoints
        {
            get { return (MaterialEfficiencyPoints + TimeEfficiencyPoints)/2.0; }
        }

        public bool IsBetterThanOther(CalibrationProgram other)
        {
            return AveragePoints > other.AveragePoints;
        }

        public virtual void CheckTargetForForgeAndThrowIfFailed(CalibrationProgram target)
        {
            Definition.ThrowIfNotEqual(target.Definition, ErrorCodes.CPRGPointsToDifferentItems);
        }

        public virtual int TargetQuantity
        {
            get { return EntityDefault.Get(TargetDefinition).Quantity; }
            set { }
        }
    }
}
