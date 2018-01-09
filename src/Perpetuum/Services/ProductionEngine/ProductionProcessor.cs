using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Services.ProductionEngine.ResearchKits;
using Perpetuum.Timers;
using Perpetuum.Zones;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionProcessor
    {
        private const int BETA_EP_MULTIPLIER = 2;
        private readonly IProductionDataAccess _productionDataAccess;
        private readonly IProductionInProgressRepository _pipRepository;
        private readonly ProductionDescription.Factory _productionDescFactory;
        private readonly IEntityServices _entityServices;
        private readonly InsuranceHelper _insuranceHelper;
        private readonly MissionProcessor _missionProcessor;

        //facility cache
        private readonly ConcurrentDictionary<long, ProductionFacility> _facilities = new ConcurrentDictionary<long, ProductionFacility>();
        private readonly Dictionary<int, ProductionDescription> _productionDescriptions = new Dictionary<int, ProductionDescription>();
        private readonly ConcurrentDictionary<int, ProductionInProgress> _productionsInProgress = new ConcurrentDictionary<int, ProductionInProgress>();

        private static Dictionary<string, object> _productionDescriptionCache;

        private readonly ConcurrentBag<NextRoundProduction> _nextRoundProductions = new ConcurrentBag<NextRoundProduction>();

        public ProductionProcessor(IProductionDataAccess productionDataAccess,
                                   IProductionInProgressRepository pipRepository,
                                   ProductionDescription.Factory productionDescFactory,
                                   IEntityServices entityServices,
                                   InsuranceHelper insuranceHelper,MissionProcessor missionProcessor)
        {
            _productionDataAccess = productionDataAccess;
            _pipRepository = pipRepository;
            _productionDescFactory = productionDescFactory;
            _entityServices = entityServices;
            _insuranceHelper = insuranceHelper;
            _missionProcessor = missionProcessor;
        }

        public void InitProcessor()
        {
            InitProductionDescriptions();
            InitLoadFacilities();
            LoadProductionsInProgress();
        }

        private void LoadProductionsInProgress()
        {
            var counter = 0;
            var canceled = 0;

            foreach (var productionInProgress in _pipRepository.GetAll())
            {
#if DEBUG
                if (!IsFacilityExists(productionInProgress.facilityEID))
                {
                    //facility not cached, skip loading the production
                    //speed up server start
                    Logger.Info("facility is not cached, skipping production load. production ID:" + productionInProgress.ID + " facilityEID:" + productionInProgress.facilityEID);
                    continue;
                }
#endif

                productionInProgress.LoadReservedItems();

                var facility = GetFacility(productionInProgress.facilityEID);
                if (facility != null)
                {
                    AddToRunningProductions(productionInProgress);
                    counter++;
                }
                else
                {
                    Logger.Error("facility not found, production cancelled. characterID:" + productionInProgress.character.Id + " ID:" + productionInProgress.ID + " facilityEID:" + productionInProgress.facilityEID);
                    try
                    {
                        CancelProduction(productionInProgress.character, productionInProgress.ID);
                        canceled++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }
            }

            Logger.Info(counter + " running productions loaded. " + canceled + " productions cancelled.");
        }

        //load the components table
        private void InitProductionDescriptions()
        {
            var loaded = 0;

            try
            {
                var definitions = Db.Query().CommandText("select distinct c.definition from components c join entitydefaults d on c.definition=d.definition WHERE d.enabled=1").Execute()
                    .Select(r => r.GetValue<int>(0)).ToList();

                foreach (var definition in definitions)
                {
                    var ed = EntityDefault.Get(definition);

                    if (ed == EntityDefault.None)
                    {
                        Logger.Error("definition not found while loading production components. definition:" + definition);
                        continue;
                    }

                    var desc = _productionDescFactory(ed.Definition);
                    _productionDescriptions.Add(definition, desc);
                    loaded++;
                }

                Logger.Info(loaded + " production descriptions loaded");
            }
            catch (Exception ex)
            {
                Logger.Error("Error occured loading the production descriptions " + ex.Message);
            }
        }

        //load ALL facilities
        private void InitLoadFacilities()
        {
            var counter = 0;
#if DEBUG
            var facilityEids = ProductionHelper.LoadFacilityEidsFromActiveZones();
#else
            var facilityEids = ProductionHelper.LoadAllLiveFacilityEids();
#endif
            foreach (var facilityEid in facilityEids)
            {
                try
                {
                    var facility = (ProductionFacility) Entity.Repository.LoadOrThrow(facilityEid);
                    facility.ProductionProcessor = this;

                    AddFacilityToCache(facility);
                    counter++;

                }
                catch (Exception ex)
                {
                    Logger.Error("error occured loading the facility " + facilityEid + " " + ex.Message);
                }

            }

            Logger.Info(counter + " production facilities cached");
        }

        public void ForceEndAllProduction()
        {
            foreach (var productionFacility in Facilities)
            {
                ForceEndProduction(productionFacility);
            }
        }

        /// <summary>
        /// Find all productions related to a facility and force end them.
        /// </summary>
        /// <param name="productionFacility"></param>
        /// <returns></returns>
        private void ForceEndProduction(ProductionFacility productionFacility)
        {
            //force end all found productions
            var foundProductions = RunningProductions.Where(pip => pip.facilityEID == productionFacility.Eid).ToList();
            EndProductions(foundProductions);
        }


        private void EndProductions(IEnumerable<ProductionInProgress> productionInProgresses, bool forced = false)
        {
            var count = 0;
            foreach (var productionInProgress in productionInProgresses)
            {
                EndProduction(productionInProgress, forced);
                count++;
            }

            if (count > 0)
            {
                Logger.Info("finishing " + count + " productions.");
            }

            EmptyNextRoundProduction();
        }

        private void EndProduction(ProductionInProgress productionInProgress, bool forced)
        {
            Logger.Info("ending production. " + productionInProgress);

            var facility = GetFacility(productionInProgress.facilityEID);
            if (facility == null)
            {
                Logger.Error("facility not found in endProduction. facility EID:" + productionInProgress.facilityEID);

                _pipRepository.Delete(productionInProgress);
                RemoveFromRunningProductions(productionInProgress);

                Logger.Error("failed endProduction for " + productionInProgress);
                return;
            }

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var replyDict = facility.EndProduction(productionInProgress, forced);

                    //delete from sql
                    _pipRepository.Delete(productionInProgress);

                    productionInProgress.SendProductionEventToCorporationMembersOnCommitted(Commands.ProductionRemoteEnd);

                    var ep =CalculateEp(facility, productionInProgress);

                    productionInProgress.character.AddExtensionPointsBoostAndLog( EpForActivityType.Production, ep);


                    if (replyDict != null)
                    {
                        Transaction.Current.OnCommited(() =>
                        {

                            //if the player is online we report the facility load
                            replyDict.Add(k.production, productionInProgress.ToDictionary());

                            var facilityInfo = facility.GetFacilityInfo(productionInProgress.character);
                            replyDict.Add(k.facility, facilityInfo);

                            Message.Builder.SetCommand(Commands.ProductionFinished)
                                .WithData(replyDict)
                                .ToCharacter(productionInProgress.character)
                                .Send();
                        });

                    }

                    productionInProgress.WriteLog();

                    Transaction.Current.OnCompleted((c) =>
                    {
                        RemoveFromRunningProductions(productionInProgress);
                    });

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

       
        private int CalculateEp(ProductionFacility facility, ProductionInProgress productionInProgress)
        {

            
            var ep = Math.Ceiling(productionInProgress.TotalProductionTime.TotalHours );

            // dev cheat
            if (productionInProgress.character.AccessLevel.IsAdminOrGm())
            {
                ep = Math.Ceiling(productionInProgress.TotalProductionTime.TotalSeconds);
            }

            var dockingBase = facility.GetDockingBase();
            if (dockingBase.Zone is TrainingZone)
                return 0;

            if (dockingBase.Zone.Configuration.IsBeta)
                ep *= BETA_EP_MULTIPLIER;

            return (int) ep;
        }

        public int GetRunningProductionsCountByFacility(long facilityEid)
        {
            return RunningProductions.Count(p => p.facilityEID == facilityEid);
        }

        public Dictionary<string, object> GetProductionsByFacilityAndCharacterToDictionary(Character character, long facilityEID)
        {
            var counter = 0;
            var replyDict = (from pip in RunningProductions.GetRunningProductionsByFacilityAndCharacter(character, facilityEID)
                select (object) pip.ToDictionary()).ToDictionary(d => "c" + counter++);

            return replyDict;
        }

        private void AddFacilityToCache(ProductionFacility productionFacility)
        {
            _facilities[productionFacility.Eid] = productionFacility;
            Logger.Info("production facility added to production cache:" + productionFacility.Eid + " " + productionFacility.ED.Name);
        }

        private bool RemoveFacilityFromCache(ProductionFacility facility)
        {
            var removed = _facilities.Remove(facility.Eid);
            if (removed)
            {
                Logger.Info("production facility removed from production cache:" + facility.Eid + " " + facility.ED.Name);
            }
            return removed;
        }

        public IEnumerable<ProductionFacility> Facilities
        {
            get { return _facilities.Select(kvp => kvp.Value); }
        }

        public bool IsFacilityExists(long facilityEid)
        {
            return _facilities.ContainsKey(facilityEid);
        }


        [CanBeNull]
        public ProductionFacility GetFacility(long facilityEid)
        {
            return _facilities.GetOrDefault(facilityEid);
        }

        public ErrorCodes GetFacility(long baseEid, ProductionFacilityType facilityType, out ProductionFacility facility)
        {
            facility = GetFacilityByBaseAndType(baseEid, facilityType);
            return facility == null ? ErrorCodes.ProductionFacilityNotFound : ErrorCodes.NoError;
        }

        [CanBeNull]
        private ProductionFacility GetFacilityByBaseAndType(long baseEid, ProductionFacilityType facilityType)
        {
            return Facilities.FirstOrDefault(f => f.Parent == baseEid && f.FacilityType == facilityType);
        }

        public IEnumerable<ProductionFacility> GetFacilitiesByBaseEid(long baseEid)
        {
            return Facilities.Where(p => p.Parent == baseEid);
        }

        public IEnumerable<ProductionFacility> GetFacilitiesByEid(IEnumerable<long> facilityEids)
        {
            return facilityEids.Select(GetFacility).Where(facility => facility != null);
        }

        public Dictionary<string, object> GetComponentsList()
        {
            if (_productionDescriptionCache == null)
            {
                _productionDescriptionCache = new Dictionary<string, object>();

                int counter = 0;
                foreach (var pdd in _productionDescriptions.Values)
                {
                    if (EntityDefault.Get(pdd.definition).CategoryFlags.IsCategory(CategoryFlags.cf_mission_items)) continue;

                    _productionDescriptionCache.Add("pd" + counter++, pdd.ToDictionary());
                }
            }

            return _productionDescriptionCache;
        }

        public void RemoveFacility(ProductionFacility facility)
        {
            Entity.Repository.Delete(facility);
            RemoveFacilityFromCache(facility);
            Logger.Info("Facility Removed eid:" + facility.Eid);
        }

        public void FacilityOnOff(long facilityEID, bool state)
        {
            var facility = GetFacility(facilityEID);
            if (facility != null)
                facility.IsOpen = state;
        }

        public IDictionary<string, object> Refine(Refinery refinery, Character character, Container sourceContainer, int targetDefinition, int amount)
        {
            refinery.IsOpen.ThrowIfFalse(ErrorCodes.FacilityClosed);
            _productionDescriptions.TryGetValue(targetDefinition, out ProductionDescription productionDescription).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);

            return refinery.Refine(character, sourceContainer, amount, productionDescription);
        }

        public IDictionary<string, object> RefineQuery(Refinery refinery, Character character, int targetDefinition, int targetAmount)
        {
            _productionDescriptions.TryGetValue(targetDefinition, out ProductionDescription productionDescription).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);
            return refinery.RefineQuery(character, targetDefinition, targetAmount, productionDescription);
        }


        public IDictionary<string, object> Reprocess(Character character, Container sourceContainer, IEnumerable<long> targetEids, Reprocessor reprocessor)
        {
            var reprocessSession = reprocessor.CollectReprocessSession(character, sourceContainer, targetEids);

            var randomComponentsDict = new Dictionary<int, int>();

            //write the result to sql
            reprocessSession.WriteSessionToSql(sourceContainer, randomComponentsDict);

            sourceContainer.Save();

            //return result
            var replyDict = new Dictionary<string, object>();
            var sourceInformData = sourceContainer.ToDictionary();

            replyDict.Add(k.sourceContainer, sourceInformData);
            replyDict.Add("randomComponents", GenerateReturningRandomComponentsResult(randomComponentsDict));

            return replyDict;
        }

        private Dictionary<string, object> GenerateReturningRandomComponentsResult(Dictionary<int, int> sumResult)
        {
            var counter = 0;
            var result = new Dictionary<string, object>(sumResult.Count);
            foreach (var pair in sumResult)
            {
                var oneEntry = new Dictionary<string, object>
                {
                    {k.definition, pair.Key},
                    {k.quantity, pair.Value}
                };
                result.Add("r" + counter++, oneEntry);
            }

            return result;
        }


        public void ScaleComponentsAmount(double scale, CategoryFlags targetCategoryFlag, CategoryFlags componentCategory)
        {
            var descriptions = _productionDescriptions.Values.Where(productionDescription => EntityDefault.Get(productionDescription.definition).CategoryFlags.IsCategory(targetCategoryFlag));

            foreach (var productionDescription in descriptions)
            {
                productionDescription.ScaleComponents(scale, componentCategory);
            }

            _productionDescriptionCache = null;
            _productionDescriptions.Clear();
            InitProductionDescriptions();
        }

        public bool IsProducible(int definition)
        {
            return _productionDescriptions.ContainsKey(definition);
        }

        [CanBeNull]
        public ProductionDescription GetProductionDescription(int definition)
        {
            return _productionDescriptions.GetOrDefault(definition);
        }

        public void CancelProduction(Character character, int id)
        {
            var pip = GetRunningProduction(id).ThrowIfNull(ErrorCodes.ItemNotFound);

            pip.HasAccess(character).ThrowIfError();

            var facility = GetFacility(pip.facilityEID).ThrowIfNull(ErrorCodes.ItemNotFound);

            //lehet-e cancelezni olyan facilityben ami zarva van? - lehessen

            var replyDict = facility.CancelProduction(pip);

            //delete from sql
            _pipRepository.Delete(pip);

            pip.SendProductionEventToCorporationMembersOnCommitted(Commands.ProductionRemoteCancel);

            Transaction.Current.OnCommited(() =>
            {
                //remove from ram
                RemoveFromRunningProductions(pip);

                if (replyDict == null)
                    return;

                replyDict.Add(k.production, pip.ToDictionary());

                var facilityInfo = facility.GetFacilityInfo(character);
                replyDict.Add(k.facility, facilityInfo);

                Message.Builder.SetCommand(Commands.ProductionCancel)
                    .WithData(replyDict)
                    .ToCharacter(pip.character)
                    .Send();
            });
        }

        [CanBeNull]
        public ProductionInProgress GetRunningProduction(int id)
        {
            return _productionsInProgress.GetOrDefault(id);
        }

        public IEnumerable<ProductionInProgress> RunningProductions
        {
            get { return _productionsInProgress.Select(kvp => kvp.Value); }
        }

        public void AddToRunningProductions(ProductionInProgress productionInProgress)
        {
            _productionsInProgress[productionInProgress.ID] = productionInProgress;
        }

        private bool RemoveFromRunningProductions(ProductionInProgress productionInProgress)
        {
            return _productionsInProgress.Remove(productionInProgress.ID);
        }

        private readonly IntervalTimer _productionCheckTimer = new IntervalTimer(TimeSpan.FromSeconds(4));

        public void Update(TimeSpan time)
        {
            _productionCheckTimer.Update(time);

            if (!_productionCheckTimer.Passed)
                return;

            _productionCheckTimer.Reset();

            CheckProductions();
        }

        public bool IsUpdateRunning()
        {
            return _inProgress;
        }


        private bool _inProgress;

        private void CheckProductions()
        {
            if (_inProgress)
                return;

            _inProgress = true;

            ProcessProductionsAsync().ContinueWith(t => { _inProgress = false; });
        }

        private Task ProcessProductionsAsync()
        {
            return Task.Run(() => ProcessProductions());
        }

        private void ProcessProductions()
        {
            var now = DateTime.Now;
            var productionInProgress = RunningProductions.Where(p => p.finishTime < now && !p.paused).ToArray();

            EndProductions(productionInProgress);
        }

        public IDictionary<string, object> ResearchItem(ResearchLab researchLab, Character character, Container container, long itemEid, long researchKitEid, bool useCorporationWallet)
        {
            var maxSlotCount = researchLab.RealMaxSlotsPerCharacter(character);
            var facilityEid = researchLab.Eid;
            var runningProductionCount = RunningProductions.GetRunningProductionsByFacilityAndCharacter(character, facilityEid).Count();

            //check per character slots
            maxSlotCount.ThrowIfLess(runningProductionCount + 1, ErrorCodes.MaximumAmountOfProducionsReached);

            var sourceItem = container.GetItemOrThrow(itemEid, true);

            //robot cargo check 
            sourceItem.GetOrLoadParentEntity().ThrowIfType<RobotInventory>(ErrorCodes.ResearchNotPossibleFromRobotCargo);

            var isPrototypeItem = _productionDataAccess.IsPrototypeDefinition(sourceItem.Definition);

            Logger.Info("item definition: " + sourceItem.ED.Name + " isPrototype:" + isPrototypeItem);

            sourceItem.HealthRatio.ThrowIfNotEqual(1.0, ErrorCodes.ItemHasToBeRepaired);

            sourceItem.Quantity.ThrowIfLess(sourceItem.ED.Quantity, ErrorCodes.MinimalQuantityNotReached);

            if (sourceItem.ED.AttributeFlags.Repackable)
            {
                sourceItem.IsRepackaged.ThrowIfFalse(ErrorCodes.ItemHasToBeRepackaged);
            }

            //CSAK csomagolt item mehet, nem kell a robotot ellenorizgetni

            var researchKit = (ResearchKit) container.GetItemOrThrow(researchKitEid, true);
            researchKit.GetOrLoadParentEntity().ThrowIfType<RobotInventory>(ErrorCodes.ResearchNotPossibleFromRobotCargo);

            //on gamma not even possible
            if (researchLab.GetDockingBase().IsOnGammaZone())
            {
                (researchKit.IsMissionRelated || sourceItem.IsCategory(CategoryFlags.cf_random_items)).ThrowIfTrue(ErrorCodes.OnlyMissionResearchKitAccepted);
            }

            //only single item
            if (researchKit.Quantity > 1)
            {
                researchKit = (ResearchKit) researchKit.Unstack(1); //this is the working piece
            }
            
            /*
            else
            {
                container.RemoveItemOrThrow(researchKit);
            }
            */

            if (sourceItem.Quantity > sourceItem.ED.Quantity)
            {
                sourceItem = sourceItem.Unstack(sourceItem.ED.Quantity);
            }
            
            /*
            else
            {
                container.RemoveItemOrThrow(sourceItem);
            }
            */

            //check item
            _productionDataAccess.IsItemResearchable(sourceItem.Definition).ThrowIfFalse(ErrorCodes.ItemNotResearchable);

            //check item using the research kit
            researchKit.IsMatchingWithItem(sourceItem).ThrowIfError();

            //match research levels
            var itemLevel = _productionDataAccess.GetResearchLevel(sourceItem.Definition);
            var researchKitLevel = researchKit.GetResearchLevel();

            _productionDataAccess.ResearchLevels.TryGetValue(sourceItem.Definition, out ItemResearchLevel itemResearchLevel).ThrowIfFalse(ErrorCodes.ItemNotResearchable);

            //calc time and bonus
            researchLab.CalculateFinalResearchTimeSeconds(character, itemLevel, researchKitLevel, isPrototypeItem, out int researchTimeSeconds, out int levelDifferenceBonusPoints);

            researchTimeSeconds = researchLab.GetShortenedProductionTime(researchTimeSeconds);

            if (researchKit.IsMissionRelated)
            {
                researchTimeSeconds = 10; //fix time for research in missions
            }

            ProductionInProgress newProduction;
            researchLab.StartResearch(character, researchTimeSeconds, sourceItem, researchKit, useCorporationWallet, out newProduction).ThrowIfError();

            //save to sql
            newProduction.InsertProductionInProgess();

            container.Save();

            //add to ram
            Transaction.Current.OnCommited(()=>AddToRunningProductions(newProduction));

            newProduction.SendProductionEventToCorporationMembersOnCommitted(Commands.ProductionRemoteStart); 

            var replyDict = new Dictionary<string, object>();

            //return info
            replyDict.Add(k.production, newProduction.ToDictionary());

            var containerInfo = container.ToDictionary();
            replyDict.Add(k.sourceContainer, containerInfo);

            //refresh the facility info
            var facilityInfo = researchLab.GetFacilityInfo(character);
            replyDict.Add(k.facility, facilityInfo);

            return replyDict;
        }


        public void CheckTargetDefinitionAndThrowIfFailed(int definition)
        {
            var ed = EntityDefault.Get(definition).ThrowIfEqual(EntityDefault.None, ErrorCodes.DefinitionNotSupported);
            //is it a refinable basic commodity?
            IsProducible(ed.Definition).ThrowIfFalse(ErrorCodes.DefinitionNotSupported);
        }

        public IDictionary<string, object> LineStartInMill(Character character, Container sourceContainer, int lineId, int cycles, bool useCorporationWallet, bool searchInRobots, Mill mill, int rounds)
        {
            const int maxCycles = 1;

            cycles.ThrowIfGreater(maxCycles, ErrorCodes.ProductionMaxCyclesExceeded);

            var maxRounds = Mill.GetMaxRounds(character);

            if (rounds > maxRounds)
            {
                rounds = maxRounds;
            }

            ProductionLine productionLine;
            ProductionLine.LoadById(lineId, out productionLine).ThrowIfError();

            productionLine.CharacterId.ThrowIfNotEqual(character.Id, ErrorCodes.OwnerMismatch);
            productionLine.IsActive().ThrowIfTrue(ErrorCodes.ProductionIsRunningOnThisLine);
            productionLine.IsAtZero().ThrowIfTrue(ErrorCodes.ProductionLineIsAtZero);

            if (productionLine.Rounds != rounds)
            {
                ProductionLine.SetRounds(rounds, productionLine.Id).ThrowIfError();
                productionLine.Rounds = rounds;
            }

            bool hasBonus;
            var newProduction = mill.LineStart(character, productionLine, sourceContainer, cycles, useCorporationWallet, out hasBonus);

            if (newProduction == null)
            {
                if (useCorporationWallet)
                {
                    throw new PerpetuumException(ErrorCodes.CorporationNotEnoughMoney);
                }

                throw new PerpetuumException(ErrorCodes.CharacterNotEnoughMoney);
            }

            //return info
            var replyDict = new Dictionary<string, object>();

            var linesList = mill.GetLinesList(character);
            replyDict.Add(k.lines, linesList);
            replyDict.Add(k.lineCount, linesList.Count);

            var productionDict = newProduction.ToDictionary();
            replyDict.Add(k.production, productionDict);

            var informDict = sourceContainer.ToDictionary();
            replyDict.Add(k.sourceContainer, informDict);

            var facilityInfo = mill.GetFacilityInfo(character);
            replyDict.Add(k.facility, facilityInfo);

            replyDict.Add(k.hasBonus, hasBonus);

            return replyDict;
        }


        public static IDictionary<string, object> LineQuery(Character character, Container container, long cprgEid, Mill mill)
        {
            var calibrationProgram = (CalibrationProgram) container.GetItemOrThrow(cprgEid);

            var targetDefinition = calibrationProgram.TargetDefinition;

            targetDefinition.ThrowIfEqual(0, ErrorCodes.CPRGNotProducible);
            var targetDefault = EntityDefault.Get(targetDefinition);

            if (calibrationProgram.IsMissionRelated || targetDefault.CategoryFlags.IsCategory(CategoryFlags.cf_random_items))
            {
                if (mill.GetDockingBase().IsOnGammaZone())
                {
                    throw new PerpetuumException(ErrorCodes.MissionItemCantBeProducedOnGamma);
                }
            }


            calibrationProgram.HasComponents.ThrowIfFalse(ErrorCodes.CPRGNotProducible);

            var replyDict = mill.QueryMaterialAndTime(calibrationProgram, character, targetDefinition, calibrationProgram.MaterialEfficiencyPoints, calibrationProgram.TimeEfficiencyPoints);

            replyDict.Add(k.materialEfficiency, calibrationProgram.MaterialEfficiencyPoints);
            replyDict.Add(k.timeEfficiency, calibrationProgram.TimeEfficiencyPoints);

            return replyDict;
        }


        public IDictionary<string, object> PrototypeStart(Character character, int targetDefinition, Container container, Prototyper prototyper, bool useCorporationWallet)
        {
            var maxSlotCount = prototyper.RealMaxSlotsPerCharacter(character);
            var facilityEid = prototyper.Eid;
            var runningProductionCount = RunningProductions.GetRunningProductionsByFacilityAndCharacter(character, facilityEid).Count();

            runningProductionCount.ThrowIfGreater(maxSlotCount - 1, ErrorCodes.MaximumAmountOfProducionsReached);

            ProductionDescription productionDescription;
            _productionDescriptions.TryGetValue(targetDefinition, out productionDescription).ThrowIfFalse(ErrorCodes.ServerError);

            bool hasBonus;
            var newProduction = prototyper.StartPrototype(character, productionDescription, container, useCorporationWallet, out hasBonus);

            //save to sql
            newProduction.InsertProductionInProgess();
            container.Save();

            //add to ram
            Transaction.Current.OnCommited(()=>AddToRunningProductions(newProduction));

            newProduction.SendProductionEventToCorporationMembersOnCommitted(Commands.ProductionRemoteStart);

            //return info
            var replyDict = new Dictionary<string, object>();

            var productionDict = newProduction.ToDictionary();
            replyDict.Add(k.production, productionDict);

            var facilityInfo = prototyper.GetFacilityInfo(character);
            replyDict.Add(k.facility, facilityInfo);

            var containerData = container.ToDictionary();
            replyDict.Add(k.sourceContainer, containerData);

            replyDict.Add(k.hasBonus, hasBonus);

            return replyDict;
        }


        public ErrorCodes PrototypeQuery(Character character, int targetDefinition, Prototyper prototyper, out Dictionary<string, object> replyDict)
        {
            var ec = ErrorCodes.NoError;
            replyDict = null;

            character.TechTreeNodeUnlocked(targetDefinition).ThrowIfFalse(ErrorCodes.TechTreeNodeNotFound);

            ProductionDescription productionDescription;
            if (!_productionDescriptions.TryGetValue(targetDefinition, out productionDescription))
            {
                Logger.Error("consistency error! no production description was found for: " + targetDefinition);
                return ErrorCodes.ServerError;
            }

            var facilityInfo = prototyper.GetFacilityInfo(character);
            var prototypeTimeSeconds = prototyper.CalculatePrototypeTimeSeconds(character, targetDefinition);
            var price = prototyper.CalculatePrototypePrice(prototypeTimeSeconds);
            bool hasBonus;
            var materialMultiplier = prototyper.CalculateMaterialMultiplier(character, targetDefinition, out hasBonus);
            var materials = ProductionDescription.GetRequiredComponentsInfo(  ProductionInProgressType.prototype, 1, materialMultiplier, productionDescription.Components.ToList());
            var prototypeDefinition = _productionDataAccess.GetPrototypePair(targetDefinition);

            replyDict = new Dictionary<string, object>
            {
                {k.materials, materials},
                {k.price, price},
                {k.productionTime, prototypeTimeSeconds},
                {k.facility, facilityInfo},
                {k.targetDefinition, prototypeDefinition},
                {k.materialEfficiency, materialMultiplier},
                {k.hasBonus, hasBonus}
            };

            return ec;
        }

        public ErrorCodes InsuranceDelete(Character character, long targetEid)
        {
            //auto clean
            InsuranceHelper.CleanUpInsurances();

            var item = Item.GetOrThrow(targetEid);

            var insurance = _insuranceHelper.GetInsurance(targetEid);

            if (insurance == null)
                return ErrorCodes.NoError;

            if (insurance.corporationEid != null)
            {
                var corporation = character.GetCorporation();
                var role = corporation.GetMemberRole(character);

                //ha nem az issuer akarja torolni
                if (insurance.character != character)
                {
                    //akkor torolhetio a CEO, dpCEO
                    if (!role.IsAnyRole(CorporationRole.DeputyCEO, CorporationRole.CEO, CorporationRole.Accountant))
                    {
                        return ErrorCodes.InsufficientPrivileges;
                    }
                }
            }
            else
            {
                //le lehet torolni ha
                // - nalad van
                // - te kototted

                if (!((item.Owner == character.Eid) || (insurance.character == character)))
                {
                    return ErrorCodes.AccessDenied;
                }
            }

            _insuranceHelper.DeleteAndInform(insurance,item.Eid);
            return ErrorCodes.NoError;
        }

        public void EnqueueProductionMissionTarget( MissionTargetType targetType, Character character,int locationId, int? definition = null, int? quantity = null)
        {
            if (locationId <= 0) return; //gamma or something unknown
           
            Logger.Info("++ Enqueue mission target type " + targetType + " characterId:" + character.Id + " definition:" + definition + " quantity:" + quantity );

            var data = new Dictionary<string, object>
                {
                    {k.characterID, character.Id},
                    {k.type, targetType},
                    {k.locationID, locationId},
                    {k.useGang, 1} //spread this action to gang within the mission engine
                };

            if (definition != null)
                data.Add(k.definition, (int) definition);

            if (quantity != null)
                data.Add(k.quantity, (int) quantity);

            _missionProcessor.EnqueueMissionTargetAsync(data);
        }

        public static void InformProductionEvent(ProductionInProgress productionInProgress, Command command)
        {
            if (!productionInProgress.useCorporationWallet)
                return;

            var replyDict = new Dictionary<string, object>();
            var productionDict = productionInProgress.ToDictionary();
            replyDict.Add(k.production, productionDict);

            const CorporationRole roleMask = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.ProductionManager | CorporationRole.Accountant;
            Message.Builder.SetCommand(command)
                .WithData(replyDict)
                .ToCorporation(productionInProgress.character.CorporationEid, roleMask)
                .Send();
        }

        public void RemovePBSDockingBase(long baseEid)
        {
            var inprogress = RunningProductions.Where(p => p.baseEID == baseEid).ToArray();
            var removedRunningProduction = inprogress.Count(RemoveFromRunningProductions);

            Logger.Info(removedRunningProduction + " production in progress out of " + inprogress.Length + " were removed from production. base eid: " + baseEid);

            var facilities = GetFacilitiesByBaseEid(baseEid).ToArray();

            foreach (var productionFacility in facilities)
            {
                productionFacility.OnRemoveFromGame();
            }

            var removedFacilityCounter = facilities.Count(RemoveFacilityFromCache);
            Logger.Info(removedFacilityCounter + " production facility out of " + facilities.Length + " were removed from production. base eid: " + baseEid);
        }

        public void AddPBSDockingBase(long baseEid)
        {
            var facilities = _entityServices.Repository.GetFirstLevelChildren_(baseEid).OfType<ProductionFacility>();
            foreach (var facility in facilities)
            {
                var ed = facility.ED;
                if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_mill) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_prototyper) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_refinery) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_repair) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_reprocessor) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_reseach_lab) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_research_kit_forge) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_calibration_program_forge) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_insurance_facility))
                {
                    AddFacilityToCache(facility);
                    Logger.Info("new pbs facilities added to production engine. " + facility.ED.Name + " " + facility.Eid);
                }
            }
        }

        public IDictionary<string, object> StartCalibrationProgramForge(Character character, long sourceEid, long targetEid, Container container, PBSCalibrationProgramForgeFacility calibrationProgramForgeFacility, bool useCorporationWallet)
        {
            calibrationProgramForgeFacility.CheckFacilitySlots(character);

            var newProduction = calibrationProgramForgeFacility.StartCPRGForge(character, sourceEid, targetEid, container, useCorporationWallet);

            //save to sql
            newProduction.InsertProductionInProgess();

            container.Save();

            //add to ram
            Transaction.Current.OnCommited(()=>AddToRunningProductions(newProduction));

            newProduction.SendProductionEventToCorporationMembersOnCommitted(Commands.ProductionRemoteStart);

            //return info

            var replyDict = new Dictionary<string, object>
            {
                {k.production, newProduction.ToDictionary()},
                {k.facility, calibrationProgramForgeFacility.GetFacilityInfo(character)},
                {k.sourceContainer, container.ToDictionary()}
            };

            return replyDict;
        }

        public IDictionary<string, object> QueryCPRGForge(Character character, long sourceEid, long targetEid, Container container, PBSCalibrationProgramForgeFacility calibrationProgramForgeFacility)
        {
            var sourceCalibration = (CalibrationProgram) container.GetItemOrThrow(sourceEid, true);
            var targetCalibration = (CalibrationProgram) container.GetItemOrThrow(targetEid, true);

            sourceCalibration.CheckTargetForForgeAndThrowIfFailed(targetCalibration);

            int materialEfficiency;
            int timeEfficiency;
            calibrationProgramForgeFacility.CalculateResultingPoints(sourceCalibration, targetCalibration, character, out materialEfficiency, out timeEfficiency);

            var forgeTimeSeconds = calibrationProgramForgeFacility.CalculateCPRGForgeTimeSeconds(character, materialEfficiency);

            var price = forgeTimeSeconds * calibrationProgramForgeFacility.GetPricePerSecond();

            var result = new Dictionary<string, object>
            {
                {k.productionTime, forgeTimeSeconds},
                {k.materialEfficiency, materialEfficiency},
                {k.timeEfficiency, timeEfficiency},
                {k.price, Math.Round(price, 0)}
            };

            return result;
        }

        public void AbortProductionsForOneCharacter(Character character)
        {
            foreach (var productionInProgress in RunningProductions.Where(pip => pip.character.Equals(character)))
            {
                Logger.Info("force removing production " + productionInProgress);
                _pipRepository.Delete(productionInProgress);
                RemoveFromRunningProductions(productionInProgress);
            }

            Logger.Info("all productions aborted for " + character);
        }


        public void EnqueueNextRoundProduction(NextRoundProduction nrp)
        {
            _nextRoundProductions.Add(nrp);
        }

        private void EmptyNextRoundProduction()
        {
            if (_nextRoundProductions.Count > 0)
            {
                Logger.Info("processing production next rounds");
            }

            var counter = 0;
            NextRoundProduction nrp;
            while (_nextRoundProductions.TryTake(out nrp))
            {
                try
                {
                    nrp.DoNextRound();
                    counter++;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }

            if (counter > 0)
            {
                Logger.Info("started " + counter + " next round productions");
            }
        }
    }
}

