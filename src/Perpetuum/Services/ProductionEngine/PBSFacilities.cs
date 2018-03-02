using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Services.ProductionEngine.ResearchKits;
using Perpetuum.Timers;

namespace Perpetuum.Services.ProductionEngine
{

    #region PBS

    public interface IPBSProductionFacility
    {
        void ProcessInfo(ProductionRefreshInfo refreshInfo);
        void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo);
    }

    public class PBSProductionFacilityHelper<T> where T : ProductionFacility, IPBSProductionFacility
    {
        private readonly T _facility;

        public PBSProductionFacilityHelper(T facility)
        {
            _facility = facility;
            
        }

        private TimeSpan _lastRefresh;
        private int _level;
        private bool _isEnabled;
        private long _pbsNodeEid;

        public void KeepAlive(ProductionRefreshInfo refreshInfo)
        {
            _lastRefresh = GlobalTimer.Elapsed;

            Logger.Info("facility enabler received. "  + refreshInfo + " " + _facility);

            _facility.SetConnected(refreshInfo.isConnected);

            if (refreshInfo.enable)
            {
                _level = refreshInfo.level;
                _pbsNodeEid = refreshInfo.senderPBSEid;
                _facility.OnPBSStartFacility();
                _isEnabled = true; //fontos a vegen, mert csak akkor nyilik ki ha elotte csukva volt!!!
                _facility.SendFacilityInfo();
            }
            else
            {
                _level = 0;
                _pbsNodeEid = 0;
                _facility.OnPBSStopFacility();
                _isEnabled = false; //fontos a vegen, mert akkor zarodik be ha nyitva volt!!!
                _facility.SendFacilityInfo();
            }
        }


        public bool IsAlive()
        {
            return _isEnabled;
        }

        public int GetProductionLevel()
        {
            return _level;
        }

        public void InitConnected()
        {
            _facility.SetConnected(false); //init pbs facility

            var baseEid = _facility.Parent;

            var records = Db.Query().CommandText("select e.definition from pbsconnections c join entities e on c.sourceeid=e.eid  where c.targeteid=@baseEid")
                                 .SetParameter("@baseEid", baseEid)
                                 .Execute();

            foreach (var record in records)
            {
                var definition = record.GetValue<int>(0);

                if (!EntityDefault.TryGet(definition, out EntityDefault ed))
                    continue;

                if (!ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_production_nodes))
                    continue;

                if (!ProductionHelper.PBSNodeCFTofacilityType.TryGetValue(ed.CategoryFlags, out ProductionFacilityType productionFacilityType))
                    continue;

                if (_facility.FacilityType == productionFacilityType)
                {
                    _facility.SetConnected(true);
                }
            }
        }


        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _facility.SetConnected(refreshInfo.isConnected);
            Logger.Info("facility connection changed. base:" + refreshInfo.targetPBSBaseEid + " enabler node:" + refreshInfo.senderPBSEid + (refreshInfo.isConnected ? " CON+++" : " DIS---") );

            _facility.SendFacilityInfo();

        }
    }




    public class PBSRefineryFacility : Refinery, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSRefineryFacility> _pbsProductionFacilityHelper;

        public PBSRefineryFacility()
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSRefineryFacility>(this);
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }


        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }

    }



    public class PBSRepairFacility : Repair, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSRepairFacility> _pbsProductionFacilityHelper;

        public PBSRepairFacility()
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSRepairFacility>(this);
        }

        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }
    }

    public class PBSMillFacility : Mill, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSMillFacility> _pbsProductionFacilityHelper;

        public PBSMillFacility()
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSMillFacility>(this);
        }

        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }


        public override bool IsOnTrainingZone()
        {
            return false;
        }

    }

    public class PBSPrototyperFacility : Prototyper, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSPrototyperFacility> _pbsProductionFacilityHelper;

        public PBSPrototyperFacility()
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSPrototyperFacility>(this);
        }

        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }


        public override bool IsOnTrainingZone()
        {
            return false;
        }

    }

    public class PBSReprocessorFacility : Reprocessor, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSReprocessorFacility> _pbsProductionFacilityHelper;

        public PBSReprocessorFacility(ReprocessSession.Factory reprocessSessionFactory) : base(reprocessSessionFactory)
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSReprocessorFacility>(this);
        }

        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }


    }

    public class PBSResearchLabFacility : ResearchLab, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSResearchLabFacility> _pbsProductionFacilityHelper;

        public PBSResearchLabFacility()
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSResearchLabFacility>(this);
        }

        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }


        public override bool IsOnTrainingZone()
        {
            return false;
        }

    }

    public class PBSResearchKitForgeFacility : ProductionFacility, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSResearchKitForgeFacility> _pbsProductionFacilityHelper;

        public PBSResearchKitForgeFacility() 
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSResearchKitForgeFacility>(this);
        }

        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var info = base.GetFacilityInfo(character);

            var additiveComponent = GetAdditiveComponent(character);

            info.Add(k.myPointsCredit, additiveComponent);
            info.Add(k.percentageCredit, GetPercentageFromAdditiveComponent(GetAdditiveComponent(character)));
            info.Add(k.extensionPoints, (int)GetMaterialExtensionBonus(character));

            return info;
        }
        
        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }

        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.ResearchKitForge; }
        }

        public override int RealMaxSlotsPerCharacter(Character characterId)
        {
            return 1;
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.DECODER_MERGE_BASIC);
        }

        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }


        private int GetAdditiveComponent(Character character)
        {
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var extensionComponent = GetMaterialExtensionBonus(character);
            var facilityComponent = GetFacilityPoint();

            return (int) (standingComponent + extensionComponent + facilityComponent);

        }


        public double GetReserchKitMergePrice(int level, Character character)
        {
            var basePrice = 1000*Math.Pow(2, level);
            
            var cost = basePrice*(1.0 + (50.0/(GetAdditiveComponent(character) + 100.0)));

            return cost;
        }



        public override bool IsOnTrainingZone()
        {
            return false;
        }

        public ErrorCodes PrepareResearchKitMerge(
            PublicContainer publicContainer,
            Character character, long target, int quantity,
            out int nextDefinition, 
            out int nextLevel,
            out double fullPrice, 
            out int availableQuantity,
            out int searchDefinition)
        {
            ErrorCodes ec;
            nextDefinition = 0;
            nextLevel = 0;
            fullPrice = 0;
            availableQuantity = 0;
            searchDefinition = 0;

            var researchKit = (ResearchKit) publicContainer.GetItem(target,true);
            if (researchKit == null)
                return ErrorCodes.ItemNotFound;

            var definition = researchKit.Definition;
            searchDefinition = definition;

            EntityDefault ed;
            if (!EntityDefault.TryGet(definition, out ed))
            {
                return ErrorCodes.DefinitionNotSupported;
            }

            int level = ed.Options.Level;
            if (level == 0)
            {
                Logger.Error("no level was defined for research kit: " + ed.Name + " " + ed.Definition);
                return ErrorCodes.ConsistencyError;
            }

            if (level == 10)
            {
                return ErrorCodes.MaximumResearchLevelReached;
            }

            nextLevel = level + 1;

            var sameDefinitions = publicContainer.GetItems().Where(i => i.Definition == definition).Sum(i => i.Quantity);

            if (sameDefinitions % 2 == 1)
            {
                sameDefinitions--;
            }

            var pairs = sameDefinitions / 2;
            
            if ((ec = ProductionHelper.FindResearchKitDefinitionByLevel(nextLevel, out nextDefinition)) != ErrorCodes.NoError)
            {
                return ec;
            }

            availableQuantity = Math.Min(pairs, quantity);

            fullPrice = GetReserchKitMergePrice(nextLevel, character) * availableQuantity;

            return ec;
        }

        public ErrorCodes DoResearchKitMerge(PublicContainer publicContainer, Character character, int nextDefinition,
            int nextLevel, double fullPrice, int availableQuantity, int searchDefinition, out Dictionary<string,object> result, bool useCorporationWallet )
        {

            var ec = ErrorCodes.NoError;
            var foundQuantity = 0;
            result = new Dictionary<string, object>();
            var amountToLookFor = availableQuantity * 2;

            foreach (var item in publicContainer.GetItems().Where(i=>i.Definition == searchDefinition))
            {
                if (item.Quantity <= amountToLookFor - foundQuantity)
                {
                    foundQuantity += item.Quantity;

                    Repository.Delete(item);
                }
                else
                {
                    item.Quantity = item.Quantity - (amountToLookFor - foundQuantity);

                    foundQuantity = amountToLookFor;
                    break;

                }

            }

            if (foundQuantity != amountToLookFor)
            {
                //safe check
                return ErrorCodes.ItemNotFound;
            }

            publicContainer.CreateAndAddItem(nextDefinition, true, item =>
            {
                item.Owner = character.Eid;
                item.Quantity = availableQuantity;
            });

            var wallet = character.GetWalletWithAccessCheck(useCorporationWallet, TransactionType.ResearchKitMerge,CorporationRole.ProductionManager);
            wallet.Balance -= fullPrice;

            var b = TransactionLogEvent.Builder()
                                       .SetTransactionType(TransactionType.ResearchKitMerge)
                                       .SetCreditBalance(wallet.Balance)
                                       .SetCreditChange(-fullPrice)
                                       .SetCharacter(character);

            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                character.LogTransaction(b);
            }

            result = new Dictionary<string, object> { {k.container, publicContainer.ToDictionary()} };
            return ec;
        }
    }


    public class PBSCalibrationProgramForgeFacility : ProductionFacility, IPBSProductionFacility
    {
        private readonly PBSProductionFacilityHelper<PBSCalibrationProgramForgeFacility> _pbsProductionFacilityHelper;

        public PBSCalibrationProgramForgeFacility()
        {
            _pbsProductionFacilityHelper = new PBSProductionFacilityHelper<PBSCalibrationProgramForgeFacility>(this);
        }

        public static PBSCalibrationProgramForgeFacility CreateWithRandomEID()
        {
            var cf = (PBSCalibrationProgramForgeFacility)Factory.CreateWithRandomEID(DefinitionNames.PBS_FACILITY_CALIBRATION_PROGRAM_FORGE);
            cf.CreateSystemStorage();
            return cf;
        
        }

        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var info = base.GetFacilityInfo(character);

            var additiveComponentTime = GetAdditiveComponentForTime(character);
            var additiveComponentPoints = GetAdditiveComponentForPoints(character);
            
            info.Add(k.myPointsTime, additiveComponentTime);
            info.Add(k.myPointsPoints, additiveComponentPoints);

            info.Add(k.timeExtensionPoints,(int)GetMaterialExtensionBonus(character));
            info.Add(k.pointsExtensionPoints, (int)GetPointExtensionBonus(character));

            info.Add(k.percentageTime, GetPercentageFromAdditiveComponent(GetAdditiveComponentForTime(character)));
            info.Add(k.percentagePoints, GetPercentageFromAdditiveComponent(GetAdditiveComponentForPoints(character)));

            return info;
        }

      

        public void ProcessInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.KeepAlive(refreshInfo);
        }

        public void ProcessConnectionInfo(ProductionRefreshInfo refreshInfo)
        {
            _pbsProductionFacilityHelper.ProcessConnectionInfo(refreshInfo);
        }


        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.CalibrationProgramForge; }
        }


        public override int GetSlotExtensionBonus(Character character)
        {
            return (int)character.GetExtensionsBonusSummary(ExtensionNames.CT_MERGE_SLOTS);
        }

        public override int RealMaxSlotsPerCharacter(Character character)
        {
            var extensionBasedAmount = GetSlotExtensionBonus(character);

            if (extensionBasedAmount == 0)
            {
                return 1;
            }

            return extensionBasedAmount;
        }

        public override bool IsOpen
        {
            get { return _pbsProductionFacilityHelper.IsAlive(); }
        }

        protected override void InitConnected()
        {
            _pbsProductionFacilityHelper.InitConnected();
        }

        public ProductionInProgress StartCPRGForge(Character character, long sourceEid, long targetEid, Container container, bool useCorporationWallet)
        {
            //check per character slots
            var maxSlotCount = RealMaxSlotsPerCharacter(character);
            var runningProductionCount = ProductionProcessor.RunningProductions.GetRunningProductionsByFacilityAndCharacter(character, Eid).Count();
            maxSlotCount.ThrowIfLess(runningProductionCount + 1, ErrorCodes.MaximumAmountOfProducionsReached);

            var sourceCalibration = (CalibrationProgram) container.GetItemOrThrow(sourceEid,true).Unstack(1);
            var targetCalibration = (CalibrationProgram) container.GetItemOrThrow(targetEid,true).Unstack(1);

            //no mission stuff
            sourceCalibration.ED.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs).ThrowIfTrue(ErrorCodes.MissionItemCantBeForged);
            targetCalibration.ED.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs).ThrowIfTrue(ErrorCodes.MissionItemCantBeForged);


            sourceCalibration.CheckTargetForForgeAndThrowIfFailed(targetCalibration);

            int materialEfficiencyPoints;
            int timeEfficiencyPoints;
            CalculateResultingPoints(sourceCalibration, targetCalibration, character, out materialEfficiencyPoints, out timeEfficiencyPoints);
            
            //put them to storage            
            sourceCalibration.Parent = StorageEid;
            targetCalibration.Parent = StorageEid;

            var targetDefinition = sourceCalibration.Definition;

            var reservedEids = new []{sourceCalibration.Eid , targetCalibration.Eid};

            var forgeTimeSeconds = CalculateCPRGForgeTimeSeconds(character,  materialEfficiencyPoints);

            forgeTimeSeconds = GetShortenedProductionTime(forgeTimeSeconds);

            var productionInProgress = ProductionInProgressFactory();
            productionInProgress.amountOfCycles = 1;
            productionInProgress.baseEID = Parent;
            productionInProgress.character = character;
            productionInProgress.facilityEID = Eid;
            productionInProgress.finishTime = DateTime.Now.AddSeconds(forgeTimeSeconds);
            productionInProgress.pricePerSecond = GetPricePerSecond(targetDefinition);
            productionInProgress.ReservedEids = reservedEids;
            productionInProgress.resultDefinition = targetDefinition;
            productionInProgress.startTime = DateTime.Now;
            productionInProgress.totalProductionTimeSeconds = forgeTimeSeconds;
            productionInProgress.type = ProductionInProgressType.calibrationProgramForge;
            productionInProgress.useCorporationWallet = useCorporationWallet;

            sourceCalibration.Save();
            targetCalibration.Save();

            if (!productionInProgress.TryWithdrawCredit())
            {
                if (useCorporationWallet)
                {
                    throw new PerpetuumException(ErrorCodes.CorporationNotEnoughMoney);
                }

                throw  new PerpetuumException(ErrorCodes.CharacterNotEnoughMoney);
            }
           

            return productionInProgress;
        }

        private int GetAdditiveComponentForTime(Character character)
        {
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var extensionComponent = GetMaterialExtensionBonus(character);
            var facilityPoints = GetFacilityPoint();

            return (int) (standingComponent + extensionComponent + facilityPoints);
        }


        public int CalculateCPRGForgeTimeSeconds(Character character, int resultedPoints)
        {
            //(1+(100/( STANDING+PLAYER_EXTENSION_PONT+Facility_PONT+100)) *researchlab.productionTime
            
            var multiplier = (1 + (100/(GetAdditiveComponentForTime(character) + 100.0)));
            
            return (int)( resultedPoints * 60 * 5 * multiplier);
        }

        public override IDictionary<string, object> EndProduction(ProductionInProgress productionInProgress, bool forced)
        {
            return EndCalibrationProgramForge(productionInProgress);
        }

        private IDictionary<string,object> EndCalibrationProgramForge(ProductionInProgress productionInProgress)
        {
            Logger.Info("Calibration program forge finished: " + productionInProgress);

            var sourceCPRGEid = productionInProgress.ReservedEids[0];
            var targetCPRGEid = productionInProgress.ReservedEids[1];

            var sourceCPRG = (CalibrationProgram)Repository.LoadOrThrow(sourceCPRGEid);
            var targetCPRG = (CalibrationProgram)Repository.LoadOrThrow(targetCPRGEid);

            //delete the used items
            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.CPRGForgeDeleted).SetCharacter(productionInProgress.character);
            foreach (var item in productionInProgress.GetReservedItems())
            {
                b.SetItem(item);
                productionInProgress.character.LogTransaction(b.Build());
                Repository.Delete(item);
            }

            //pick the output defintion---------------------------------------------------

            var outputDefinition = productionInProgress.resultDefinition;

            //load container
            var container = (PublicContainer) Container.GetOrThrow(PublicContainerEid);
            container.ReloadItems(productionInProgress.character);

            var resultItem = (CalibrationProgram) Factory.CreateWithRandomEID(outputDefinition);
            resultItem.Owner = productionInProgress.character.Eid;
            resultItem.Save();

            int materialEfficiencyPoints;
            int timeEfficiencyPoints;
            CalculateResultingPoints(sourceCPRG, targetCPRG, productionInProgress.character, out materialEfficiencyPoints, out timeEfficiencyPoints);

            resultItem.MaterialEfficiencyPoints = materialEfficiencyPoints;
            resultItem.TimeEfficiencyPoints = timeEfficiencyPoints;

            //add to public container
            container.AddItem(resultItem, false);
            container.Save();

            productionInProgress.character.WriteItemTransactionLog(TransactionType.PrototypeCreated, resultItem);

            //get list in order to return

            Logger.Info("EndCPGForge created an item: " + resultItem + " production:" + productionInProgress);

            var replyDict = new Dictionary<string, object>
                            {
                                {k.result, resultItem.ToDictionary()},
                            };

            return replyDict;
        }

        public override double GetMaterialExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.CT_MERGE_TIME);
        }

        private static double GetPointExtensionBonus(Character character)
        {
            return character.GetExtensionsBonusSummary(ExtensionNames.CT_MERGE_BASIC);
        }


        protected override int GetFacilityPoint()
        {
            return _pbsProductionFacilityHelper.GetProductionLevel();
        }

        private int GetAdditiveComponentForPoints(Character character)
        {
            var extensionComponent = GetPointExtensionBonus(character);
            var standingComponent = GetStandingOfOwnerToCharacter(character) * 20;
            var facilityPoints = GetFacilityPoint();

            return (int) (extensionComponent + standingComponent + facilityPoints);
        }


       
        public void CalculateResultingPoints(CalibrationProgram sourceCalibration, CalibrationProgram targetCalibration,Character character, out int materialEfficiencyPoints, out int timeEfficiencyPoints)
        {
            //betterCT.ME+weakCT.ME/3 + (1-(100/(100+STANDING+PLAYER_EXTENSION_PONT+Facility_PONT)))*25
            
            var materialBase = 0;
            var timeBase = 0;
            var weakMaterial = 0;
            var weakTime = 0;


            if (sourceCalibration.IsBetterThanOther(targetCalibration))
            {
                materialBase = sourceCalibration.MaterialEfficiencyPoints;
                timeBase = sourceCalibration.TimeEfficiencyPoints;
                weakMaterial = targetCalibration.MaterialEfficiencyPoints /3;
                weakTime = targetCalibration.TimeEfficiencyPoints /3;
            }
            else
            {
                materialBase = targetCalibration.MaterialEfficiencyPoints;
                timeBase = targetCalibration.TimeEfficiencyPoints;
                weakMaterial = sourceCalibration.MaterialEfficiencyPoints /3;
                weakTime = sourceCalibration.TimeEfficiencyPoints/3;
            }

            var magic = (1 - (100.0/(100.0 + GetAdditiveComponentForPoints(character))))*50;

            materialEfficiencyPoints = (int) (materialBase + weakMaterial + magic);
            timeEfficiencyPoints = (int) (timeBase + weakTime + magic);

        }

        public override IDictionary<string, object> CancelProduction(ProductionInProgress productionInProgress)
        {
            return ReturnReservedItems(productionInProgress);
        }


        public override bool IsOnTrainingZone()
        {
            return false;
        }
    }


    #endregion

}
