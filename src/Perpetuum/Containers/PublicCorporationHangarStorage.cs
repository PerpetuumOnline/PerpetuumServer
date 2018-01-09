using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Containers
{
    /// <summary>
    /// System level storage to store all CorporateHangar containers on a docking base
    /// practically this is the parent of every corporate hangar, one per base
    /// </summary>
    public class PublicCorporationHangarStorage : Container
    {
        private readonly CorporationConfiguration _corporationConfiguration;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public PublicCorporationHangarStorage(CorporationConfiguration corporationConfiguration,DockingBaseHelper dockingBaseHelper)
        {
            _corporationConfiguration = corporationConfiguration;
            _dockingBaseHelper = dockingBaseHelper;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void AddItem(Item item, long issuerEid, bool doStack)
        {
            throw new PerpetuumException(ErrorCodes.AccessDenied);
        }

        public struct CorporationHangarRentInfo
        {
            public int price;
            public TimeSpan period;
        }

        public CorporationHangarRentInfo GetCorporationHangarRentInfo()
        {
            var saveNeeded = false;

            if (!DynamicProperties.Contains(k.hangarPrice))
            {
                DynamicProperties.Update(k.hangarPrice,_corporationConfiguration.HangarPrice);
                saveNeeded = true;
            }

            if (!DynamicProperties.Contains(k.rentPeriod))
            {
                DynamicProperties.Update(k.rentPeriod,_corporationConfiguration.RentPeriod);
                saveNeeded = true;
            }

            if (saveNeeded)
            {
                this.Save();
            }

            return new CorporationHangarRentInfo
            {
                price = DynamicProperties.GetOrAdd<int>(k.hangarPrice), 
                period = TimeSpan.FromDays(DynamicProperties.GetOrAdd<int>(k.rentPeriod))
            };
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            var rentInfo = GetCorporationHangarRentInfo();

            info[k.rentPrice] = rentInfo.price;
            info[k.rentPeriod] = rentInfo.period;

            return info;
        }

        [CanBeNull]
        public DockingBase GetParentDockingBase()
        {
            return _dockingBaseHelper.GetDockingBase(Parent);
        }

    }
}
