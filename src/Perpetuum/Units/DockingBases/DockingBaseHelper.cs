using System.Collections.Generic;
using System.Linq;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.ItemShop;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Zones;
using Perpetuum.Zones.Training;

namespace Perpetuum.Units.DockingBases
{
    public class UnitHelper
    {
        private readonly IZoneManager _zoneManager;
        private readonly ItemHelper _itemHelper;

        public UnitHelper(IZoneManager zoneManager,ItemHelper itemHelper)
        {
            _zoneManager = zoneManager;
            _itemHelper = itemHelper;
        }

        public T GetUnitOrThrow<T>(long unitEid) where T : Unit
        {
            var unit = GetUnit<T>(unitEid);
            if (unit == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);
            return unit;
        }

        public T GetUnit<T>(long unitEid) where T : Unit
        {
            if (unitEid == 0)
                return null;

            var unit = _zoneManager.GetUnit<T>(unitEid);
            if (unit == null)
            {
                Logger.DebugWarning($"{typeof(T).Name} not found on zone! eid:{unitEid}");
            }

            return unit;
        }

        public Unit LoadUnit(long unitEid)
        {
            return (Unit) _itemHelper.LoadItem(unitEid);
        }

        public Unit LoadUnitOrThrow(long unitEid)
        {
            return (Unit) _itemHelper.LoadItemOrThrow(unitEid);
        }

        public Unit CreateUnit(EntityDefault entityDefault, EntityIDGenerator idGenerator)
        {
            return (Unit) _itemHelper.CreateItem(entityDefault, idGenerator);
        }

        public Unit CreateUnit(int definition, EntityIDGenerator idGenerator)
        {
            return (Unit) _itemHelper.CreateItem(definition, idGenerator);
        }
    }

	public class DockingBaseHelper
	{
	    private readonly IEntityServices _entityServices;
	    private readonly IZoneManager _zoneManager;
	    private readonly UnitHelper _unitHelper;

	    public DockingBaseHelper(IEntityServices entityServices,IZoneManager zoneManager,UnitHelper unitHelper)
	    {
	        _entityServices = entityServices;
	        _zoneManager = zoneManager;
	        _unitHelper = unitHelper;
	    }

        [CanBeNull]
	    public DockingBase GetDockingBase(long dockingBaseEid)
        {
            return _unitHelper.GetUnit<DockingBase>(dockingBaseEid);
	    }

	    public TrainingDockingBase GetTrainingDockingBase()
	    {
	        return _zoneManager.Zones.GetUnits<TrainingDockingBase>().FirstOrDefault();
	    }

	    public IEnumerable<DockingBase> GetDefaultDockingBases()
	    {
	        return GetAllPublicDockingBases().Where(b => !(b.Zone is TrainingZone));
	    }

        public IEnumerable<DockingBase> GetAllPublicDockingBases()
	    {
	        return _zoneManager.Zones.GetUnits<DockingBase>()
	            .Where(b => b.IsCategory(CategoryFlags.cf_public_docking_base));
        }

        public Entity GetStationService(Entity serviceBase, EntityDefault serviceEntityDefault)
        {
            return _entityServices.Repository.GetChildByDefinition(serviceBase, serviceEntityDefault.Definition);
		}

	    [CanBeNull]
	    public PublicContainer GetPublicContainer(Entity dockingBase)
	    {
            return (PublicContainer) GetStationService(dockingBase, _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_CONTAINER));
	    }

	    [CanBeNull]
	    public Market GetMarket(Entity dockingBase)
	    {
	        var market = _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_MARKET);
	        return (Market)GetStationService(dockingBase,market);
	    }

	    [CanBeNull]
	    public ItemShop GetItemShop(Entity dockingBase)
	    {
	        var itemShop = _entityServices.Defaults.GetByName(DefinitionNames.BASE_ITEM_SHOP);
	        return (ItemShop)GetStationService(dockingBase,itemShop);
	    }

	    [CanBeNull]
	    public PublicCorporationHangarStorage GetPublicCorporationHangarStorage(Entity dockingBase)
	    {
	        var storage = _entityServices.Defaults.GetByName(DefinitionNames.PUBLIC_CORPORATE_HANGARS_STORAGE);
	        return (PublicCorporationHangarStorage)GetStationService(dockingBase,storage);
	    }

	    public IEnumerable<ProductionFacility> GetProductionFacilities(Entity dockingBase)
	    {
	        var childrenEid = _entityServices.Repository.GetFirstLevelChildrenByCategoryflags(dockingBase.Eid, CategoryFlags.cf_production_facilities);

	        foreach (var facilityEid in childrenEid)
	        {
	            yield return (ProductionFacility)_entityServices.Repository.LoadOrThrow(facilityEid);
	        }
	    }


    }
}