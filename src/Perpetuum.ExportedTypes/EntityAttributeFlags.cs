namespace Perpetuum.ExportedTypes
{
    public struct EntityAttributeFlags
    {
        public EntityAttributeFlags(ulong flags) : this()
        {
            Flags = flags;
        }

        public ulong Flags { get; private set; }

        public bool HasFlag(AttributeFlags flag)
        {
            return (Flags & ((ulong)1 << (int)flag)) > 0;
        }

        public void SetFlag(AttributeFlags flag)
        {
            Flags |= (ulong)1 << (int) flag;
        }

        public bool ActiveModule
        {
            get { return HasFlag(AttributeFlags.activeModule); }
        }

        public bool AlwaysStackable
        {
            get { return HasFlag(AttributeFlags.alwaysStackable); }
        }

        public bool Consumable
        {
            get { return HasFlag(AttributeFlags.consumable); }
        }

        public bool ForceOneCycle
        {
            get { return HasFlag(AttributeFlags.forceOneCycle); }
        }

        public bool Invulnerable
        {
            get { return HasFlag(AttributeFlags.invulnerable); }
        }

        public bool MainBase
        {
            get { return HasFlag(AttributeFlags.mainbase); }
        }

        public bool NonAttackable
        {
            get { return HasFlag(AttributeFlags.nonattackable); }
        }

        public bool NonLockable
        {
            get { return HasFlag(AttributeFlags.nonlockable); }
        }

        public bool NonRecyclable
        {
            get { return HasFlag(AttributeFlags.nonRecyclable); }
        }

        public bool NonRelocatable
        {
            get { return HasFlag(AttributeFlags.nonrelocatable); }
        }

        public bool NonStackable
        {
            get { return HasFlag(AttributeFlags.nonStackable); }
        }

        public bool OffensiveModule
        {
            get { return HasFlag(AttributeFlags.offensive_module); }
        }

        public bool PassiveModule
        {
            get { return HasFlag(AttributeFlags.passiveModule); }
        }

        public bool PrimaryLockedTarget
        {
            get { return HasFlag(AttributeFlags.primary_locked_target); }
        }

        public bool PvpSupport
        {
            get { return HasFlag(AttributeFlags.pvp_support); }
        }

        public bool SelfEffect
        {
            get { return HasFlag(AttributeFlags.self_effect); }
        }

        public bool TargetIsRobot
        {
            get { return HasFlag(AttributeFlags.targetIsRobot); }
        }

        public bool Repackable
        {
            get { return !AlwaysStackable && !NonStackable; }
        }

        public bool InstantActivate
        {
            get { return HasFlag(AttributeFlags.instantActivate); }
        }
    }
}