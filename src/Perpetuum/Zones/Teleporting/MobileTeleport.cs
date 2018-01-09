using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.Teleporting
{
    public class MobileTeleport : Teleport
    {
        private readonly TeleportDescriptionBuilder.Factory _descriptionBuilderFactory;
        private UnitDespawnHelper _despawnHelper;
        private TimeSpan _cooldownInterval;

        public MobileTeleport(TeleportDescriptionBuilder.Factory descriptionBuilderFactory)
        {
            _descriptionBuilderFactory = descriptionBuilderFactory;
        }

        public override void AcceptVisitor(TeleportVisitor visitor)
        {
            visitor.VisitMobileTeleport(this);
        }

        public override ErrorCodes IsAttackable => ErrorCodes.NoError;

        public override bool IsLockable
        {
            get { return true; }
        }

        public void SetCooldownInterval(TimeSpan interval)
        {
            _cooldownInterval = interval;
        }

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this,despawnTime);
        }

        public override IEnumerable<TeleportDescription> GetTeleportDescriptions()
        {
            var zone = Zone;
            if (zone == null)
                return new TeleportDescription[0];

            var descriptionId = 0;
            var teleportColumns = zone.GetTeleportColumns().Where(t => t.IsEnabled);

            var result = teleportColumns.Select(teleportColumn =>
            {
                var builder = _descriptionBuilderFactory();
                builder.SetId(descriptionId++)
                .SetType(TeleportDescriptionType.WithinZone)
                .SetSourceTeleport(this)
                .SetSourceZone(zone)
                .SetSourceRange(TeleportRange)
                .SetTargetZone(zone)
                .SetTargetTeleport(teleportColumn)
                .SetTargetRange(TeleportRange)
                .SetActive(true)
                .SetListable(true);
                return builder.Build();
            }).ToArray();

            return result;
        }

        private readonly TimeSpan _mobileTeleportDeployDelay = TimeSpan.FromMinutes(3);

        public void ApplyTeleportCooldownEffect(bool first = false)
        {
            var builder = NewEffectBuilder().SetType(EffectType.effect_teleport_cooldown).WithDuration(first ? _mobileTeleportDeployDelay : _cooldownInterval);
            ApplyEffect(builder);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            if (enterType == ZoneEnterType.Deploy)
            {
                ApplyTeleportCooldownEffect(true); //az elso kornek a cooldownja
            }

            base.OnEnterZone(zone, enterType);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            var dictionary = GetTeleportDescriptions().ToDictionary("t",d => d.ToDictionary());
            result.Add(k.targets, dictionary);
            return result;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _despawnHelper.Update(time, this);
        }

        public virtual void CheckDeploymentAndThrow(IZone zone, Position spawnPosition )
        {
            zone.Units.OfType<DockingBase>().WithinRange(spawnPosition, DistanceConstants.MOBILE_TELEPORT_MIN_DISTANCE_TO_DOCKINGBASE).Any().ThrowIfTrue(ErrorCodes.MobileTeleportsAreNotDeployableNearBases);
            zone.Units.OfType<Teleport>().WithinRange(spawnPosition, DistanceConstants.MOBILE_TELEPORT_MIN_DISTANCE_TO_TELEPORT).Any().ThrowIfTrue(ErrorCodes.TeleportIsInRange);
            

        }
    }

}