using Perpetuum.Zones;

namespace Perpetuum.Units
{
    partial class Unit
    {
        public UnitStates States { get; private set; }
        
        public class UnitStates
        {
            private static readonly UnitStateFlags[] _damageLevels = { UnitStateFlags.damage_level25, UnitStateFlags.damage_level50, UnitStateFlags.damage_level75 };
            private const UnitStateFlags DAMAGE_LEVEL_MASK = ~(UnitStateFlags.damage_level25 | UnitStateFlags.damage_level50 | UnitStateFlags.damage_level75);

            private readonly Unit _unit;
            private UnitStateFlags _flags = UnitStateFlags.undefined;

            public UnitStates(Unit unit)
            {
                _unit = unit;
                _unit._armor.PropertyChanged += property => OnArmorChanged();
            }

            public bool Dock
            {
                get { return HasFlag(UnitStateFlags.dock); }
                set { SetFlag(UnitStateFlags.dock, value); }
            }

            public bool Teleport
            {
                get { return HasFlag(UnitStateFlags.teleport); }
                set { SetFlag(UnitStateFlags.teleport, value); }
            }

            public bool LocalTeleport
            {
                get { return HasFlag(UnitStateFlags.local_teleport); }
                set { SetFlag(UnitStateFlags.local_teleport, value); }
            }

            public bool Dead
            {
                get { return HasFlag(UnitStateFlags.dead); }
                set { SetFlag(UnitStateFlags.dead, value); }
            }

            public bool Combat
            {
                get { return HasFlag(UnitStateFlags.combat); }
                set { SetFlag(UnitStateFlags.combat, value); }
            }

            public bool InMoveable
            {
                get { return HasFlag(UnitStateFlags.inMovable); }
                set { SetFlag(UnitStateFlags.inMovable, value); }
            }

            public bool Aggressive
            {
                get { return HasFlag(UnitStateFlags.aggressive); }
                set { SetFlag(UnitStateFlags.aggressive, value); }
            }

            public bool Unlockable
            {
                get { return HasFlag(UnitStateFlags.unlockable); }
                set { SetFlag(UnitStateFlags.unlockable, value); }
            }

            public bool Online
            {
                get { return HasFlag(UnitStateFlags.isOnline); }
                set { SetFlag(UnitStateFlags.isOnline, value); }
            }

            public bool Open
            {
                get { return !Online; }
                set { Online = !value; }
            }

            public bool IsOrphaned
            {
                get { return HasFlag(UnitStateFlags.isOrphaned); }
                set { SetFlag(UnitStateFlags.isOrphaned, value);}
            }


            public bool Reinforced
            {
                get { return HasFlag(UnitStateFlags.reinforced); }
                set { SetFlag(UnitStateFlags.reinforced, value); }
            }

            public bool BigBrother
            {
                get { return HasFlag(UnitStateFlags.bigbrother); }
                set { SetFlag(UnitStateFlags.bigbrother, value); }
            }

            public bool LockSomething
            {
                get { return HasFlag(UnitStateFlags.lock_something); }
                set { SetFlag(UnitStateFlags.lock_something, value); }
            }

            public bool UseEnabled
            {
                get { return HasFlag(UnitStateFlags.useEnabled); }
                set { SetFlag(UnitStateFlags.useEnabled, value); }
            }

            public bool MineralScan
            {
                get { return HasFlag(UnitStateFlags.mineral_scan); }
                set { SetFlag(UnitStateFlags.mineral_scan, value); }
            }

            private void OnArmorChanged()
            {
                var flags = _flags & DAMAGE_LEVEL_MASK;

                var index = (int)((_unit.Armor.Ratio(_unit.ArmorMax)) * 4);
                if (index < 3)
                {
                    flags |= _damageLevels[index];
                }

                if (_flags == flags)
                    return;

                _flags = flags;
                _unit.UpdateTypes |= UnitUpdateTypes.State;
            }

            private bool HasFlag(UnitStateFlags flag)
            {
                return (_flags & flag) > 0;
            }

            public void SetFlag(UnitStateFlags flag, bool value)
            {
                var flags = _flags;

                if (value)
                {
                    flags |= flag;
                }
                else
                {
                    flags &= ~flag;
                }

                if (_flags == flags)
                    return;

                _flags = flags;
                _unit.UpdateTypes |= UnitUpdateTypes.State;
            }

            public override string ToString()
            {
                return _flags.ToString();
            }

            public void AppendToPacket(Packet packet)
            {
                packet.AppendLong((long)_flags);
            }
        }

    }
}
