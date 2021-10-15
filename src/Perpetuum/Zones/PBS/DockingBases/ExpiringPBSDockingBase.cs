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
        private TimeSpan _aliveElapsed;
        private readonly TimeSpan _alertPeriod = TimeSpan.FromHours(1);
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

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, LifeTime);
            _despawnHelper.DespawnStrategy = Kill;

            base.OnEnterZone(zone, enterType);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            if (!_alerted)
                WarnIfAboutToExpire(time);

            _despawnHelper?.Update(time, this);

            base.OnUpdate(time);
        }

        private void WarnIfAboutToExpire(TimeSpan time)
        {
            _aliveElapsed += time;
            if (LifeTime - _aliveElapsed < _alertPeriod)
            {
                Task.Run(() => _pbsObjectHelper.SendAttackAlert());
                _alerted = true;
            }
        }

        public override ErrorCodes IsDeconstructAllowed()
        {
            return ErrorCodes.DockingBaseNotSetToDeconstruct;
        }
    }
}
