using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.ProductionEngine
{

    #region outpost stuff

    public class OutpostMill : Mill
    {

        private readonly int[] _millPoints = { 50, 100, 125, 150 };

        protected override int GetFacilityPoint()
        {
            var bonusSection = Outpost.GetFacilityLevelFromStack(Eid);
            return _millPoints[bonusSection];
        }


    }

    public class OutpostRefinery : Refinery
    {
        private readonly int[] _refineryPoints = { 50, 100, 125, 150 };

        protected override int GetFacilityPoint()
        {
            var bonusSection = Outpost.GetFacilityLevelFromStack(Eid);

            return _refineryPoints[bonusSection];
        }

    }

    public class OutpostPrototyper : Prototyper
    {
        private readonly int[] _prototyperPoints = { 50, 100, 125, 150 };

        protected override int GetFacilityPoint()
        {
            var bonusSection = Outpost.GetFacilityLevelFromStack(Eid);
            return _prototyperPoints[bonusSection];
        }
    }

    public class OutpostRepair : Repair
    {
        private readonly int[] _repairPoints = { 50, 100, 125, 150 };

        protected override int GetFacilityPoint()
        {
            var bonusSection = Outpost.GetFacilityLevelFromStack(Eid);
            return _repairPoints[bonusSection];
        }


    }

    public class OutpostReprocessor : Reprocessor
    {
        private readonly int[] _reprocessorPoins = { 50, 100, 125, 150 };

        public OutpostReprocessor(ReprocessSession.Factory reprocessSessionFactory) : base(reprocessSessionFactory)
        {
        }

        protected override int GetFacilityPoint()
        {
            var bonusSection = Outpost.GetFacilityLevelFromStack(Eid);
            return _reprocessorPoins[bonusSection];
        }

    }

    public class OutpostResearchLab : ResearchLab
    {
        private readonly int[] _researchLabPoints = { 50, 100, 125, 150 };

        protected override int GetFacilityPoint()
        {
            var bonusSection = Outpost.GetFacilityLevelFromStack(Eid);
            return _researchLabPoints[bonusSection];
        }
    }

    #endregion

}
