using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;

namespace Perpetuum.Items
{
    public struct ItemInfo : IEquatable<ItemInfo>
    {
        public static readonly ItemInfo None = new ItemInfo(0,0);

        public int Definition { get; private set; }
        public int Quantity { get; set; }
        // min/max quantity for loot generation
        public int MinQty { get; set; }
        public int MaxQty { get; set; }
        //
        public float Health { get; set; }
        public bool IsRepackaged { get; set; }

        public ItemInfo(int definition,int quantity = 1) : this()
        {
            Definition = definition;
            Quantity = quantity;
            Health = (float) EntityDefault.Health;
            IsRepackaged = EntityDefault.AttributeFlags.Repackable;
        }

        // overload, accept min/max quantities for loot items.
        public ItemInfo(int definition, int minq = 1, int maxq = 1) : this()
        {
            Definition = definition;
            MinQty = minq;
            MaxQty = maxq;
            Quantity = this.randomQuantity(); //randomize quantity for min-max ranged loots on init
            Health = (float)EntityDefault.Health;
            IsRepackaged = EntityDefault.AttributeFlags.Repackable;
        }

        //Roll random
        public int randomQuantity()
        {
            if(this.MinQty != this.MaxQty)
            {
                return FastRandom.NextInt(this.MinQty, this.MaxQty);
            }
            return this.MinQty;
        }

        public EntityDefault EntityDefault
        {
            get { return EntityDefault.Get(Definition); }
        }

        public double Volume
        {
            get { return EntityDefault.CalculateVolume(IsRepackaged,Quantity); }
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.definition,Definition},
                    {k.quantity,Quantity}
                };
        }

        public override string ToString()
        {
            return $"Definition: {Definition}, Quantity: {Quantity}, Health: {Health}, IsRepackaged: {IsRepackaged}";
        }

        public bool Equals(ItemInfo other)
        {
            return Definition == other.Definition && Quantity == other.Quantity && Health.Equals(other.Health) && IsRepackaged.Equals(other.IsRepackaged);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ItemInfo && Equals((ItemInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Definition;
                hashCode = (hashCode*397) ^ Quantity;
                hashCode = (hashCode*397) ^ Health.GetHashCode();
                hashCode = (hashCode*397) ^ IsRepackaged.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ItemInfo left, ItemInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemInfo left, ItemInfo right)
        {
            return !left.Equals(right);
        }
    }
}