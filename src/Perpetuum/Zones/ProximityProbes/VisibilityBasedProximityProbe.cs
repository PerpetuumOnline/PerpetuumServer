using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Zones.Blobs.BlobEmitters;

namespace Perpetuum.Zones.ProximityProbes
{
    public class VisibilityBasedProximityProbe : ProximityProbeBase , IBlobEmitter
    {
        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        public override List<Player> GetNoticedUnits()
        {
            return GetVisibleUnits().Select(v=>v.Target).OfType<Player>().ToList();
        }

        protected override bool IsActive
        {
            get
            {
                var coreRatio = Core.Ratio(CoreMax);
                return coreRatio > 0.98;
            }
        }

        public double BlobEmission
        {
            get
            {
                var blobEmission = GetPropertyModifier(AggregateField.blob_emission);
                return blobEmission.Value;
            }
        }

        public double BlobEmissionRadius
        {
            get
            {
                var blobEmissionRadius = GetPropertyModifier(AggregateField.blob_emission_radius);
                return blobEmissionRadius.Value;
            }
        }

        public override void OnInsertToDb()
        {
            DynamicProperties.Update(k.currentCore, Core);
            base.OnInsertToDb();
        }

        public override void OnUpdateToDb()
        {
            DynamicProperties.Update(k.currentCore, Core);
            base.OnUpdateToDb();
        }
    }
}