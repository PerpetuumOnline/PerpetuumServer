using System;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Modules
{
    public class BlobEmissionModulatorModule : ActiveModule
    {
        private const int    BLOB_EMITTER_DEFINITION = 2870;
        private const int    BLOB_EMITTER_HEIGHT = 7;
        private const double BLOB_EMITTER_DEPLOY_RANGE = 15;

        private readonly BlobEmissionProperty _blobEmission;
        private readonly BlobEmissionRadiusProperty _blobEmissionRadius;

        public BlobEmissionModulatorModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);
            _blobEmission = new BlobEmissionProperty(this);
            AddProperty(_blobEmission);
            _blobEmissionRadius = new BlobEmissionRadiusProperty(this);
            AddProperty(_blobEmissionRadius);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.blob_emission:
                    {
                        _blobEmission.Update();
                        break;
                    }
                case AggregateField.blob_emission_radius:
                    {
                        _blobEmissionRadius.Update();
                        break;
                    }
            }

            base.UpdateProperty(field);
        }

        protected override void OnAction()
        {
            var zone = Zone;
            if (zone == null)
                return;

            Position? lockPosition = null;

            var myLock = GetLock();
            if (myLock is TerrainLock)
            {
                lockPosition = (myLock as TerrainLock).Location.AddToZ(BLOB_EMITTER_HEIGHT);
            }
            else if (myLock is UnitLock)
            {
                lockPosition = (myLock as UnitLock).Target.CurrentPosition.AddToZ(BLOB_EMITTER_HEIGHT);
            }
            else
            {
                OnError(ErrorCodes.InvalidLockType);
                return;
            }

            Position targetPosition = lockPosition.Value;

            zone.Units.OfType<BlobEmitterUnit>().WithinRange(targetPosition, BLOB_EMITTER_DEPLOY_RANGE).Any().ThrowIfTrue(ErrorCodes.BlobEmitterInRange);
            var r = zone.IsInLineOfSight(ParentRobot, targetPosition, false);
            if (r.hit)
            {
                OnError(ErrorCodes.LOSFailed);
                return;
            }

            var blobEmitter = (BlobEmitterUnit)Factory.CreateWithRandomEID(BLOB_EMITTER_DEFINITION);
            blobEmitter.Initialize();
            var ammo = GetAmmo();
            var despawnTimeMod = ammo.GetPropertyModifier(AggregateField.despawn_time);
            blobEmitter.DespawnTime = TimeSpan.FromMilliseconds(despawnTimeMod.Value);
            blobEmitter.BlobEmission = _blobEmission.Value;
            blobEmitter.BlobEmissionRadius = _blobEmissionRadius.Value;
            if (ParentRobot is Player player)
                blobEmitter.Owner = player.Character.Eid;

            var finder = new ClosestWalkablePositionFinder(zone, targetPosition);
            var position = finder.FindOrThrow();

            var beamBuilder = Beam.NewBuilder()
                .WithType(BeamType.deploy_device_01)
                .WithPosition(targetPosition)
                .WithDuration(TimeSpan.FromSeconds(5));

            blobEmitter.AddToZone(zone, position, ZoneEnterType.Default, beamBuilder);

            ConsumeAmmo();
        }

        private class BlobEmissionProperty : ModuleProperty
        {
            private readonly BlobEmissionModulatorModule _module;

            public BlobEmissionProperty(BlobEmissionModulatorModule module) : base(module, AggregateField.blob_emission)
            {
                _module = module;
            }

            protected override double CalculateValue()
            {
                var ammo = _module.GetAmmo();
                if (ammo == null)
                    return 0.0;

                var blobEmission = ammo.GetPropertyModifier(AggregateField.blob_emission);
                var blobEmissionMod = module.GetPropertyModifier(AggregateField.blob_emission_modifier);
                blobEmissionMod.Modify(ref blobEmission);

                if (module.ParentRobot != null)
                {
                    var blobEmissionModMod = module.ParentRobot.GetPropertyModifier(AggregateField.blob_emission_modifier_modifier);
                    blobEmissionModMod.Modify(ref blobEmission);
                }

                return blobEmission.Value;
            }
        }

        private class BlobEmissionRadiusProperty : ModuleProperty
        {
            private readonly BlobEmissionModulatorModule _module;

            public BlobEmissionRadiusProperty(BlobEmissionModulatorModule module) : base(module, AggregateField.blob_emission_radius)
            {
                _module = module;
            }

            protected override double CalculateValue()
            {
                var ammo = _module.GetAmmo();
                if (ammo == null)
                    return 0.0;

                var blobEmissionRadius = ammo.GetPropertyModifier(AggregateField.blob_emission_radius);
                var blobEmissionRadiusMod = module.GetPropertyModifier(AggregateField.blob_emission_radius_modifier);
                blobEmissionRadiusMod.Modify(ref blobEmissionRadius);

                if (module.ParentRobot != null)
                {
                    var robotBlobEmissionRadiusMod = module.ParentRobot.GetPropertyModifier(AggregateField.blob_emission_radius_modifier);
                    robotBlobEmissionRadiusMod.Modify(ref blobEmissionRadius);
                }

                return blobEmissionRadius.Value;
            }
        }
    }
}
