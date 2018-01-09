using System;
using System.Collections.Immutable;
using Perpetuum.Items;

namespace Perpetuum.Units
{
    public class UnitUpdatedEventArgs : EventArgs
    {
        public UnitUpdateTypes UpdateTypes { get; internal set; }
        public ImmutableHashSet<ItemProperty> UpdatedProperties { get; internal set; }
    }
}