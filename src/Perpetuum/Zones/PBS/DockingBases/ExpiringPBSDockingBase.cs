using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items.Templates;
using Perpetuum.Services.Channels;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using System;
using System.Threading.Tasks;

namespace Perpetuum.Zones.PBS.DockingBases
{
    public class ExpiringPBSDockingBase : PBSDockingBase
    {
        private IUnitDespawnHelper _despawnHelper;
        private readonly TimeSpan _alertPeriod = TimeSpan.FromHours(1);
        private readonly TimeSpan _minLife = TimeSpan.FromMinutes(5);
        private const int MIN_LIFE_HOURS = 72;
        private bool _alerted;

        public ExpiringPBSDockingBase(
            MarketHelper marketHelper,
            ICorporationManager corporationManager,
            IChannelManager channelManager,
            ICentralBank centralBank,
            IRobotTemplateRelations robotTemplateRelations,
            DockingBaseHelper dockingBaseHelper,
            SparkTeleportHelper sparkTeleportHelper,
            PBSObjectHelper<PBSDockingBase>.Factory pbsObjectHelperFactory) : base(marketHelper,
             corporationManager,
             channelManager,
             centralBank,
             robotTemplateRelations,
             dockingBaseHelper,
             sparkTeleportHelper,
             pbsObjectHelperFactory)
        {
        }

        public TimeSpan LifeTime
        {
            get
            {
                return TimeSpan.FromHours(ED.Config.lifeTime ?? MIN_LIFE_HOURS);
            }
        }

        public DateTime EndTime
        {
            get
            {
                return DynamicProperties.GetOrAdd(k.endTime, () => DateTime.Now + LifeTime);
            }
        }

        public TimeSpan Remaining
        {
            get
            {
                return (EndTime - DateTime.Now).Max(_minLife);
            }
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, Remaining);
            _despawnHelper.DespawnStrategy = (unit) =>
            {
                ReinforceHandler.ReinforceCounter = 0;
                ReinforceHandler.CurrentState.ToVulnerable();
                Kill();
            };

            base.OnEnterZone(zone, enterType);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            WarnIfAboutToExpire();

            _despawnHelper?.Update(time, this);

            base.OnUpdate(time);
        }

        private void WarnIfAboutToExpire()
        {
            if (!_alerted && Remaining < _alertPeriod)
            {
                _alerted = true;
                Task.Run(() => _pbsObjectHelper.SendAttackAlert());
            }
        }

        public override ErrorCodes IsDeconstructAllowed()
        {
            return ErrorCodes.DockingBaseNotSetToDeconstruct;
        }

        public override ErrorCodes SetDeconstructionRight(Character issuer, bool state)
        {
            DynamicProperties.Remove(k.allowDeconstruction);
            return ErrorCodes.DockingBaseNotSetToDeconstruct;
        }
    }
}
