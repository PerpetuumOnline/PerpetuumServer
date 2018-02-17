using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.Standing;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public abstract class ProductionFacility : Entity
    {
        public abstract ProductionFacilityType FacilityType { get; }

        public abstract int RealMaxSlotsPerCharacter(Character character);

        protected ProductionFacility()
        {
            _isOnTrainingZone = new Lazy<bool>(LoadIsOnTrainingZone);
            IsConnected = true;
        }

        public IProductionDataAccess ProductionDataAccess { get; set; }
        public IStandingHandler StandingHandler { get; set; }
        public ProductionProcessor ProductionProcessor { get; set; }
        public MissionDataCache MissionDataCache { get; set; }
        public DockingBaseHelper DockingBaseHelper { get; set; }
        public ProductionInProgress.Factory ProductionInProgressFactory { get; set; }
        public RobotHelper RobotHelper { protected get; set; }        

        public override string ToString()
        {
            return $"{ED.Name} {ED.Definition} {Eid}";
        }

        private int GetFacilityBonus()
        {
            int modifier = 0;
            var dockingbase = GetDockingBase();
            if (dockingbase is Outpost)
            {
                ProductionFacility facility = (dockingbase as Outpost).GetProductionFacilities().Where(x => x.Eid == this.Eid).First();
                modifier = Outpost.GetFacilityLevelFromStack(facility.Eid) * 25;
            }
            return modifier;
        }

        public virtual Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoData = BaseInfoToDictionary();

            infoData.Add(k.open, IsOpen);
            infoData.Add(k.standingPoints, GetStandingPoints(character));
            infoData.Add(k.maxSlots, RealMaxSlotsPerCharacter(character));
            infoData.Add(k.facilityPoints, GetFacilityPoint());
            infoData.Add(k.isConnected, IsConnected);

            return infoData;
        }

        public override void OnLoadFromDb()
        {
            InitConnected();
            base.OnLoadFromDb();
        }

        protected virtual void InitConnected(){}

        public bool IsConnected { get; private set; }

        public void SetConnected(bool state)
        {
            IsConnected = state;
            Logger.Info( (state ? "++CONNECT++" : "--DISCONNECT--") +  " " +  FacilityType + " " + ED.Name + " " + ED.Definition);
        }

        private long _storageEid;

        protected long StorageEid
        {
            get
            {
                if (_storageEid == 0)
                {
                    _storageEid = ProductionHelper.LoadStorageEid(Eid);

#if DEBUG
                    if (_storageEid == 0)
                    {
                        CreateSystemStorage();
                        this.Save();
                        Logger.Info("a system storage was created to correct facility: " + this);
                    }
#endif
                }
                return _storageEid;
            }
            private set { _storageEid = value; }
        }

        private long _publicContainerEid;

        protected long PublicContainerEid
        {
            get
            {
                if (_publicContainerEid == 0)
                {
                    _publicContainerEid = Repository.GetFirstLevelChildrenByCategoryflags(Parent, CategoryFlags.cf_public_container).FirstOrDefault();
                    Logger.Info("public container " + _publicContainerEid + " loaded for production facility" + Eid);
                }

                return _publicContainerEid;
            }
        }

        protected override Entity LoadParentEntity(long parent)
        {
            return DockingBaseHelper.GetDockingBase(parent);
        }

        [NotNull]
        public DockingBase GetDockingBase()
        {
            var dockingBase = DockingBaseHelper.GetDockingBase(Parent);
            return dockingBase;
        }

        public virtual bool IsOnTrainingZone()
        {
            return _isOnTrainingZone.Value;
        }
        
        private readonly Lazy<bool> _isOnTrainingZone;

        private bool LoadIsOnTrainingZone()
        {
            return GetDockingBase().Zone is TrainingZone;
        }

        protected virtual int GetFacilityPoint()
        {
            return ED.Options.Points + GetFacilityBonus();
        }

        protected int GetPercentageFromAdditiveComponent(int additiveComponent)
        {
            return (int) ((1 - (100.0/(additiveComponent + 100.0))) * 100);
        }

        private int _productionTimeSeconds;

        protected virtual int GetProductionTimeSeconds()
        {
            if (_productionTimeSeconds == 0)
            {
                var productionTime = ED.Options.ProductionTime;

                if (productionTime == 0)
                {
                    Logger.Error("no production time defined for facility. definition: " + Definition);
                    productionTime = 3600;
                }

                _productionTimeSeconds = productionTime;
            }

            return _productionTimeSeconds;

        }

        public virtual double GetMaterialExtensionBonus(Character character)
        {
            return 0.0;
        }
        
        public virtual double GetTimeExtensionBonus(Character character)
        {
            return 0.0;
        }
        
        public virtual int GetSlotExtensionBonus(Character character)
        {
            return 0;
        }

        public int GetShortenedProductionTime(int originalProductionTime)
        {
            if (IsOnTrainingZone())
            {
                return 30;
            }

            return originalProductionTime;
        }

        private bool _isOpen = true;
      

        public virtual bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                Logger.Info("facility state set. facility eid: " + Eid + " state:" + value);
                _isOpen = value;
            }
        }

        private int GetStandingPoints(Character character)
        {
            return (int)( GetStandingOfOwnerToCharacter(character)*20);
        }

        public virtual double GetPricePerSecond()
        {
            var pricePerSecond = ED.Options.PerSecondPrice;
            if (pricePerSecond <= 0)
            {
                Logger.Error("no price per second is defined for facility: " + Eid + " definition:" + Definition);
                pricePerSecond = 3.0;
            }

            return pricePerSecond;
        }

        protected void CreateSystemStorage()
        {
            var storage = DefaultSystemContainer.Create();

            storage.Owner = Owner;
            
            AddChild(storage);

            StorageEid = storage.Eid; //cache storage eid
        }

        protected void RemoveStorage()
        {
            Repository.DeleteTree(StorageEid);
        }

        protected void MoveItemsToStorage(params Item[] itemsToReserve)
        {
            StorageEid.ThrowIfEqual(0,ErrorCodes.ServerError);

            foreach (var item in itemsToReserve)
            {
                item.Parent = StorageEid;
                item.Save();
            }
        }

        public IDictionary<string,object> ReturnReservedItems(ProductionInProgress productionInProgress)
        {
            StorageEid.ThrowIfEqual(0,ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var replyDict = new Dictionary<string, object>(1);

            var itemsDict = new Dictionary<string, object>();

            var counter = 0;
            //put the reserved items into the public container
            foreach (var item in productionInProgress.GetReservedItems())
            {
                item.Parent = PublicContainerEid;
                item.Save();

                itemsDict.Add("r" + counter++, item.ToDictionary());
            }

            replyDict.Add(k.items, itemsDict);
            return replyDict;
        }


        protected double GetStandingOfOwnerToCharacter(Character character)
        {
            var facilityOwner = Owner;
            if (facilityOwner > 0)
            {
                if (DefaultCorporationDataCache.IsAllianceDefault(facilityOwner))
                {
                    //simply the player vs the alliance
                    return StandingHandler.GetStanding(facilityOwner, character.Eid);

                    //return StandingHandler.Instance.GetIndustrialDefaultCorporationStandingByAlliance(character.Eid, facilityOwner);
                }

                return StandingHandler.GetStandingServerEntityToPlayerHierarchy(facilityOwner, character);
            }

            Logger.Error("no owner defined for production facility:" + this);
            return 0.0;
        }

        public virtual IDictionary<string, object> EndProduction(ProductionInProgress productionInProgress,bool forced)
        {
            return null;
        }

        public virtual IDictionary<string, object> CancelProduction(ProductionInProgress productionInProgress)
        {
            return null;
        }

        public void CheckFacilitySlots(Character character)
        {
            var runningProductionCount = ProductionProcessor.RunningProductions.GetRunningProductionsByFacilityAndCharacter(character, Eid).Count();
            var maxSlotCount = RealMaxSlotsPerCharacter(character);
            maxSlotCount.ThrowIfLess(runningProductionCount + 1,ErrorCodes.MaximumAmountOfProducionsReached);
        }

        public void SendFacilityInfo()
        {
            var baseEid = Parent;

            var charactersToInform = Db.Query().CommandText("select characterid from characters where active=1 and baseeid=@baseEid and docked=1")
                .SetParameter("@baseEid", baseEid)
                .Execute()
                .Select(r => Character.Get(r.GetValue<int>(0)))
                .ToArray();

            var result = new Dictionary<string, object>
            {
                {k.baseEID, baseEid},
                {k.facility, Eid},
                {k.open, IsOpen},
                {k.isConnected, IsConnected}
            };


            Message.Builder.SetCommand(Commands.ProductionFacilityState).WithData(result).ToCharacters(charactersToInform).Send();
        }

        public virtual void OnRemoveFromGame() {}

        public int MyMissionLocationId()
        {
            var entity = this.GetOrLoadParentEntity().ThrowIfNull(ErrorCodes.ConsistencyError);

            var location = MissionDataCache.GetLocationByEid(entity.Eid);

            var locationId = -1; //not a mission location, gamma base for example
            if (location != null)
            {
                locationId = location.id;
            }

            return locationId;
        }

        public SystemContainer GetStorage()
        {
            return (SystemContainer) Repository.Load(StorageEid);
        }

    }
}