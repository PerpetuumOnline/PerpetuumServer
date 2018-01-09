using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Zones.Blobs.BlobEmitters;

namespace Perpetuum.Zones.Teleporting
{
    public class TeleportColumn : Teleport,IBlobEmitter
    {
        private readonly ITeleportDescriptionRepository _teleportDescriptionRepository;
        private const double TELEPORT_BLOB_EMISSION = 2.0;
        private const double TELEPORT_BLOB_EMISSION_RADIUS = 50.0;

        private readonly IDynamicProperty<int> _isEnabled;

        public TeleportColumn(ITeleportDescriptionRepository teleportDescriptionRepository)
        {
            _teleportDescriptionRepository = teleportDescriptionRepository;
            _isEnabled = DynamicProperties.GetProperty<int>(k.enabled);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override ErrorCodes IsAttackable => ErrorCodes.TargetIsNonAttackable;

        protected override void OnRemovedFromZone(IZone zone)
        {
            zone.UnitService.RemoveDefaultUnit(this,false);

            Repository.Delete(this);

            //itt pucolhatna teleport channeleket is
            //talan jobb, ha nem teszi. kisebb a pusztitas, a load meg intezi ugyis

            base.OnRemovedFromZone(zone);
        }

        [CanBeNull]
        private int[] GetSourceList()
        {
            var descriptions = _teleportDescriptionRepository.GetAll();
            return descriptions.Where(d => d.listable && d.SourceTeleport?.Eid == Eid).Select(d => d.id).ToArray();
        }

        public override bool IsEnabled
        {
            get { return _isEnabled.Value.ToBool(); }
            set
            {
                _isEnabled.Value = value.ToInt();
                States.UseEnabled = value;
            }
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);

            if (!IsEnabled)
            {
                Logger.Info("teleport column is turned ON " + this);
                IsEnabled = true;
                this.Save();
            }

            States.UseEnabled = IsEnabled;
        }

        public override IEnumerable<TeleportDescription> GetTeleportDescriptions()
        {
            return _teleportDescriptionRepository.SelectMany(GetSourceList());
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            var sourceList = GetSourceList();

            if ( sourceList != null )
            {
                dictionary.Add(k.channels,sourceList);
            }

            return dictionary;
        }

        public double BlobEmission
        {
            get { return TELEPORT_BLOB_EMISSION; }
        }

        public double BlobEmissionRadius
        {
            get { return TELEPORT_BLOB_EMISSION_RADIUS; }
        }
    }
}