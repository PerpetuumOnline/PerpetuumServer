using Perpetuum.Accounting.Characters;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.Services.ProductionEngine
{
    /// <summary>
    /// Helper class to star a next production
    /// </summary>
    public class NextRoundProduction
    {
        private readonly Character character;
        private readonly int productionLineId;
        private readonly int cycles;
        private readonly bool useCorporationWallet;
        private readonly long facilityEid;
        private readonly ProductionProcessor processor;

        public NextRoundProduction(ProductionProcessor productionProcessor, Character ownerCharacter, int lineId, int amountOfCycles, bool useCorpWallet, long facility)
        {
            processor = productionProcessor;
            character = ownerCharacter;
            productionLineId = lineId;
            cycles = amountOfCycles;
            useCorporationWallet = useCorpWallet;
            facilityEid = facility;
        }


        public void DoNextRound()
        {
            var facility = processor.GetFacility(facilityEid);
            var mill = facility as Mill;
            mill?.TryNextRound(character, productionLineId, cycles, useCorporationWallet);
        }
    }
}