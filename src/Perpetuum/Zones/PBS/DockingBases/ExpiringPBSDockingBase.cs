using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items.Templates;
using Perpetuum.Services.Channels;
using Perpetuum.Services.Mail;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Zones.PBS.DockingBases
{
    /// <summary>
    /// This is a special PBS base that expires based on a configured lifetime
    /// It can not be deconstructed, it must expire or be killed
    /// There are also special rules about placement which are enforced in the PBSDeployer module
    /// The expiration status of the base is communicate to players by mail and terminal chat messages and MOTD
    /// </summary>
    public class ExpiringPBSDockingBase : PBSDockingBase
    {
        private IUnitDespawnHelper _despawnHelper;
        private readonly TimeSpan _minLifeOnInit = TimeSpan.FromMinutes(10);
        private bool _firstUpdate = true;
        private const string SENDER_CHARACTER_NICKNAME = "[OPP] Announcer";
        private readonly Character _announcer;

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
            _announcer = Character.GetByNick(SENDER_CHARACTER_NICKNAME);
        }

        public TimeSpan LifeTime
        {
            get
            {
                return TimeSpan.FromHours(ED.Config.lifeTime ?? 72);
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
                return (EndTime - DateTime.Now).Max(_minLifeOnInit);
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
            _firstUpdate = true;

            base.OnEnterZone(zone, enterType);
        }

        private void OnFirst()
        {
            _firstUpdate = false;
            ChannelManager.JoinChannel(ChannelName, _announcer, ChannelMemberRole.Operator, null);
            ChannelManager.SetTopic(ChannelName, _announcer, $"Base Expires at {EndTime.ToString("F")}");
            SendMailStatusAsync();
        }

        protected override void OnUpdate(TimeSpan time)
        {
            if (_firstUpdate)
                OnFirst();

            _despawnHelper?.Update(time, this);

            base.OnUpdate(time);
        }

        public override void JoinChannel(Character character)
        {
            base.JoinChannel(character);
            ChannelManager.Announcement(ChannelName, _announcer, $"Base is going to expire in: {Remaining.ToHumanTimeString()}");
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

        /// <summary>
        /// Get authorized managers of the facility
        /// </summary>
        /// <returns>IEnumerable<Character></returns>
        private IEnumerable<Character> AuthorizedCorpOfficers()
        {
            var corp = Corporation.Get(Owner);
            if (corp == null)
                return new Character[] { };

            return corp.GetMembersWithAnyRoles(CorporationRole.DeputyCEO,
                CorporationRole.CEO,
                CorporationRole.editPBS).Select(m => m.character);
        }

        /// <summary>
        /// Send mail to the top 10 most active authorized corp officers
        /// </summary>
        /// <returns>Task</returns>
        private Task SendMailStatusAsync()
        {
            return Task.Run(() =>
            {
                var officers = AuthorizedCorpOfficers().OrderByDescending(c => c.LastLogout).Take(10);
                var message = BuildStatusMessage();
                var subject = "Syntec Base Expiration info";
                using (var scope = Db.CreateTransaction())
                {
                    foreach (var officer in officers)
                    {
                        MailHandler.SendMail(_announcer, officer, subject, BuildStatusMessage(), MailType.storyteller, out _, out _);
                    }
                    scope.Complete();
                }
            });
        }

        private string BuildStatusMessage()
        {
            return $"Your Syntec Base will expire at {EndTime.ToString("F")} in {Remaining.ToHumanTimeString()}.\n\nBase will become unstable and self-destruct at this time!\nPlease plan your stay accordingly.";
        }
    }
}
