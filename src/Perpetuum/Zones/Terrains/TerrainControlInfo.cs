using System;

namespace Perpetuum.Zones.Terrains
{
    [Serializable]
    public struct TerrainControlInfo : IEquatable<TerrainControlInfo>
    {
        private TerrainControlFlags _flags;

        public TerrainControlFlags Flags
        {
            get { return _flags; }
        }

        public bool AntiPlant
        {
            get { return HasFlags(TerrainControlFlags.AntiPlant); }
        }

        public bool Roaming
        {
            get { return HasFlags(TerrainControlFlags.Roaming); }
            set { SetFlags(TerrainControlFlags.Roaming,value); }
        }

        public bool Highway
        {
            get { return HasFlags(TerrainControlFlags.Highway); }
            set { SetFlags(TerrainControlFlags.Highway, value); }
        }

        public bool PBSHighway
        {
            get { return HasFlags(TerrainControlFlags.PBSHighway); }
            set { SetFlags(TerrainControlFlags.PBSHighway, value);}
        }

        public bool IsAnyHighway
        {
            get { return ((int)_flags & (int) TerrainControlFlags.HighWayCombo) > 0; }
        }

        public bool SyndicateArea
        {
            get { return HasFlags(TerrainControlFlags.SyndicateArea); }
        }

        public bool TerraformProtected
        {
            get {return HasFlags(TerrainControlFlags.TerraformProtected); }
            set { SetFlags(TerrainControlFlags.TerraformProtected,value); }
        }

        public bool PBSTerraformProtected
        {
            get { return HasFlags(TerrainControlFlags.PBSTerraformProtected); }
            set { SetFlags(TerrainControlFlags.PBSTerraformProtected, value); }
        }

        public bool NpcRestricted
        {
            get { return HasFlags(TerrainControlFlags.NpcRestricted); }
        }

        public bool IsAnyTerraformProtected
        {
            get { return ((int)_flags & (int)TerrainControlFlags.TerraformProtectedCombo) > 0; }
        }

        public bool PlantAllowed
        {
            get { return !(AntiPlant || Roaming || IsAnyHighway || AnyConcrete); }
        }

        public bool DevrinolAllowed
        {
            get { return !AntiPlant; }
        }

        public bool ConcreteA
        {
            private get { return _flags.HasFlag(TerrainControlFlags.ConcreteA); }
            set { SetFlags(TerrainControlFlags.ConcreteA, value); }
        }

        public bool ConcreteB
        {
            private get { return _flags.HasFlag(TerrainControlFlags.ConcreteB); }
            set { SetFlags(TerrainControlFlags.ConcreteB,value); }
        }

        private bool HasFlags(TerrainControlFlags flags)
        {
            return ((int)_flags & ((int)flags)) > 0;
        }

        private void SetFlags(TerrainControlFlags f,bool value)
        {
            _flags = value ? (TerrainControlFlags)((ushort)_flags | (ushort)f) : (TerrainControlFlags)((ushort)_flags & ~((ushort)f));
        }

        public bool AnyConcrete
        {
            get { return ConcreteA || ConcreteB; }
        }

        public void ClearAllConcrete()
        {
            ConcreteA = false;
            ConcreteB = false;
        }

        public bool Equals(TerrainControlInfo other)
        {
            return _flags == other._flags;
        }
    }
}