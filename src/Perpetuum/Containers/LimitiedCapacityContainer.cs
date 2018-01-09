using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;

namespace Perpetuum.Containers
{
    /// <summary>
    /// Base class for containers with limited capacity
    /// </summary>
    public class LimitedCapacityContainer : Container
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        private double FreeCapacity
        {
            get { return Capacity - Load; }
        }

        private double Capacity
        {
            get { return ED.Options.Capacity; }
        }

        private double Load
        {
            get
            {
                var load = GetItems().Sum(i => i.Volume);
                return load;
            }
        }

        public void CheckCapacityAndThrowIfFailed()
        {
            var load = Load;
            load.ThrowIfGreater(Capacity,ErrorCodes.OutOfCargo,gex => gex.SetData("capacity", Capacity).SetData("currentLoad", load));
        }

        public bool IsEnoughCapacity(Item item)
        {
            return IsEnoughCapacity(item.Volume);
        }

        private bool IsEnoughCapacity(double volume)
        {
            return Capacity >= volume + Load;
        }

        public override void AddItem(Item item,long issuerEid, bool doStack)
        {
            //no infinite capacity containers
            if (item.IsCategory(CategoryFlags.cf_infinite_capacity_containers))
            {
                item.ThrowIfNotType<VolumeWrapperContainer>(ErrorCodes.InfiniteCapacityContainerNotSupported);
            }

            if (!IsEnoughCapacity(item))
            {
                var load = Load;
                Logger.Info("item is not fitting in container. eid:" + Eid + " load:" + load + " item volume:" + item.Volume + " maxCapacity:" + Capacity + " volumeEmpty:" + (Capacity - load) + " item definition:" + item.Definition + " " + item.ED.Name + " repackaged:" + item.IsRepackaged + " quantity:" + item.Quantity + " \n" + SystemTools.GetCallStack());
                throw new PerpetuumException(ErrorCodes.ContainerIsFull);
            }

            base.AddItem(item, issuerEid, doStack);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.load, Load);
            return result;
        }

        public int GetMaximalQuantity(EntityDefault itemEd, int desiredQuantity)
        {
            var quantityFits = (int)Math.Floor(FreeCapacity / itemEd.Volume);
            return desiredQuantity.Clamp(0, quantityFits);
        }

        public Dictionary<string, object> GetCapacityInfo()
        {
            var info = new Dictionary<string, object>()
            {
                {k.capacity, Capacity},
                {k.load, Load}
            };
            return info;
        }

    }
}
