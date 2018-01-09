using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;

namespace Perpetuum.Zones.Blobs.BlobEmitters
{
    public class BlobEmitter : IBlobEmitter
    {
        private readonly ItemProperty _blobEmission;
        private readonly ItemProperty _blobEmissionRadius;

        public BlobEmitter(Unit unit)
        {
            _blobEmission = new UnitProperty(unit, AggregateField.blob_emission, AggregateField.blob_emission_modifier, AggregateField.effect_blob_emission_modifier);
            unit.AddProperty(_blobEmission);

            _blobEmissionRadius = new UnitProperty(unit, AggregateField.blob_emission_radius, AggregateField.blob_emission_radius_modifier, AggregateField.effect_blob_emission_radius_modifier);
            unit.AddProperty(_blobEmissionRadius);
        }

        public double BlobEmission
        {
            get { return _blobEmission.Value; }
        }

        public double BlobEmissionRadius
        {
            get { return _blobEmissionRadius.Value; }
        }
    }
}