using System;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionManager : IProcess
    {
        public ProductionProcessor ProductionProcessor { get; private set; }

        public ProductionManager(ProductionProcessor productionProcessor)
        {
            ProductionProcessor = productionProcessor;
        }

        public void Start()
        {
            Logger.Info("Production engine started. ");
        }

        public void Stop()
        {
            if (ProductionProcessor.IsUpdateRunning())
            {
                while (ProductionProcessor.IsUpdateRunning())
                {
                    Logger.Info("waiting for productions to finish");
                    Thread.Sleep(2000);
                }
            }

            Logger.Info("all productions are finished.");
        }

        public void Update(TimeSpan time)
        {
            ProductionProcessor.Update(time);
        }

        public TFacility GetFacility<TFacility>(long facilityEid) where TFacility : ProductionFacility
        {
            return (ProductionProcessor.GetFacility(facilityEid) as TFacility).ThrowIfNull(ErrorCodes.FacilityTypeMismatch);
        }

        public void GetFacilityAndCheckDocking<TFacility>(long facilityEID, Character character, out TFacility facility) where TFacility : ProductionFacility
        {
            facility = GetFacility<TFacility>(facilityEID);

            facility.IsOpen.ThrowIfFalse(ErrorCodes.FacilityClosed);

            //only docked
            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

            //only from current base
            character.CurrentDockingBaseEid.ThrowIfNotEqual(facility.Parent, ErrorCodes.FacilityOutOfReach);
        }

        public void PrepareProductionForPublicContainer<TFacility>(long facilityEid, Character character, out TFacility facility, out PublicContainer container) where TFacility : ProductionFacility
        {
            GetFacilityAndCheckDocking(facilityEid, character, out facility);
            container = character.GetPublicContainerWithItems();
        }

        public void RefreshPBSFacility(ProductionRefreshInfo refreshData)
        {
            ProductionProcessor.GetFacility(refreshData.targetPBSBaseEid, refreshData.facilityType, out ProductionFacility productionFacility).ThrowIfError();

            var pbsFacility = productionFacility as IPBSProductionFacility;
            pbsFacility?.ProcessInfo(refreshData);
        }

        public void PBSFacilityConnected(ProductionRefreshInfo refreshData)
        {
            ProductionProcessor.GetFacility(refreshData.targetPBSBaseEid, refreshData.facilityType, out ProductionFacility productionFacility).ThrowIfError();

            var pbsFacility = productionFacility as IPBSProductionFacility;
            pbsFacility?.ProcessConnectionInfo(refreshData);
        }

        public void RemovePBSBase(ProductionRefreshInfo refreshData)
        {
            //remove all prod facility and related stuff from caches, sqlt intezi a pbs ondead, mindent torol, lootosit
            var baseEid = refreshData.targetPBSBaseEid;
            ProductionProcessor.RemovePBSDockingBase(baseEid);
        }

        public void AddPBSBase(ProductionRefreshInfo refreshData)
        {
            //load facilities from sql, add them to cache
            var baseEid = refreshData.targetPBSBaseEid;
            ProductionProcessor.AddPBSDockingBase(baseEid);
        }
    }
}