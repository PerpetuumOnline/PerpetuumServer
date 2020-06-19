using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;

namespace Perpetuum.Zones.Blobs.BlobEmitters
{
    public class BlobEmitterUnit : Unit, IBlobEmitter
    {
        private double _blobEmission;
        private double _blobEmissionRadius;
        private UnitDespawnHelper _despawnHelper;

        public bool IsPlayerSpawned => Owner != 0;

        public TimeSpan DespawnTime
        {
            set { _despawnHelper = UnitDespawnHelper.Create(this, value); }
        }

        public override bool IsLockable
        {
            get { return true; }
        }

        public double BlobEmission
        {
            get { return _blobEmission; }
            set
            {
                var x = GetPropertyModifier(AggregateField.blob_emission_modifier);
                x.Modify(ref value);
                _blobEmission = value;
            }
        }

        public double BlobEmissionRadius
        {
            get { return _blobEmissionRadius; }
            set
            {
                var x = GetPropertyModifier(AggregateField.blob_emission_radius_modifier);
                x.Modify(ref value);
                _blobEmissionRadius = value;
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            _despawnHelper?.Update(time, this);
        }
    }

}