using Perpetuum.Services.ProductionEngine;

namespace Perpetuum.Zones.PBS.ProductionNodes
{

    public class PBSRefineryEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.Refine;
        }
    }

    public class PBSRepairEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.Repair;
        }
    }


    public class PBSMillEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.MassProduce;
        }
    }

    public class PBSPrototyperEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.Prototype;
        }
    }

    public class PBSResearchLabEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.Research;
        }
    }

    public class PBSReprocessEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.Reprocess;
        }
    }

    public class PBSCalibrationProgramForgeEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.CalibrationProgramForge;
        }
    }

    public class PBSResearchKitForgeEnablerNode : PBSProductionFacilityNode
    {
        public override ProductionFacilityType GetFacilityType()
        {
            return ProductionFacilityType.ResearchKitForge;
        }
    }
}
