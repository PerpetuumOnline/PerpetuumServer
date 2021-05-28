using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones.Gates
{
    public class Gate : Unit, IUsableItem, IHaveStandingLimit
    {
        private readonly IStandingHandler _standingHandler;
        private readonly IDynamicProperty<int> _openState;
        private readonly IDynamicProperty<double> _standingLimit;
        private int _using;

        public Gate(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
            _openState = DynamicProperties.GetProperty(k.state, () => 1);
            _standingLimit = DynamicProperties.GetProperty(k.standing, () => 0.0);
        }

        private bool IsOpen
        {
            get { return _openState.Value.ToBool(); }
            set { _openState.Value = value.ToInt(); }
        }

        public double StandingLimit
        {
            get { return _standingLimit.Value; }
            set { _standingLimit.Value = value; }
        }


        /// <summary>
        /// Used with genxy
        /// </summary>
        /// <param name="character"></param>
        /// <param name="corporationEid"></param>
        public void UseGateWithCharacter(Character character, long corporationEid)
        {
            if (Interlocked.CompareExchange(ref _using, 1, 0) == 1)
            {
                // mar hasznalja valaki..esetleg error mehetne vissza
                return;
            }

            try
            {
                // elmentjuk az aktualis state-et
                var currentState = IsOpen;

                HasAccess(corporationEid).ThrowIfFalse(ErrorCodes.AccessDenied);

                if (IsOpen)
                {
                    GetUnitsWithinRange2D(this.GetItemWorkRangeOrDefault() + 1).OfType<Player>().Any().ThrowIfTrue(ErrorCodes.PlayerInGateArea);
                }

                using (var scope = Db.CreateTransaction())
                {
                    // megforditjuk
                    IsOpen = !currentState;
                    this.Save();

                    Transaction.Current.OnCompleted(commited =>
                    {
                        if (!commited)
                        {
                            // ha nem volt commit akkor a regi state kerul vissza (mini undo)
                            IsOpen = currentState;
                        }
                        else
                        {
                            Message.Builder.SetCommand(Commands.UseItem).WithData(ToDictionary()).ToCharacter(character).Send();
                        }
                    });

                    scope.Complete();
                }
            }
            finally
            {
                // akarmi tortent az alapjan nyitjuk/zarjuk
                OpenOrClose(Zone, IsOpen);
                Interlocked.Exchange(ref _using, 0);
            }
        }


        /// <summary>
        /// used with zone packet
        /// </summary>
        /// <param name="player"></param>
        public void UseItem(Player player)
        {
           UseGateWithCharacter(player.Character, player.CorporationEid);
        }

        private bool HasAccess(Player player)
        {
            return HasAccess(player.CorporationEid);
        }
        

        private bool HasAccess(long playersCorporationEid )
        {
            if (playersCorporationEid == Owner)
            {
                //own corporation -> ok
                return true;
            }

            if (DefaultCorporationDataCache.IsCorporationDefault(playersCorporationEid))
            {
                //only private corporation members
                return false;
            }

            var standingValue = _standingHandler.GetStanding(Owner, playersCorporationEid);

            //match standing level
            return standingValue > StandingLimit;
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var debugInfo = base.GetDebugInfo();
            debugInfo[k.open] = IsOpen;
            debugInfo[k.standing] = StandingLimit;
            return debugInfo;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            info[k.open] = IsOpen;
            info[k.standing] = StandingLimit;
            return info;
        }


        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            OpenOrClose(zone, IsOpen);
            base.OnEnterZone(zone, enterType);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            DeleteAndCleanUp(zone);
            base.OnRemovedFromZone(zone);
        }

        private void DeleteAndCleanUp(IZone zone)
        {
            using (var scope = Db.CreateTransaction())
            {
                zone.UnitService.RemoveUserUnit(this);

                Transaction.Current.OnCommited(() =>
                {
                    OpenOrClose(zone, true);
                });

                scope.Complete();
            }
        }

        private void OpenOrClose(IZone zone, bool open)
        {
            if (zone == null)
                return;

            States.Open = open;

            var bi = new BlockingInfo();
            if (open)
            {
                bi.Obstacle = false;
                bi.Height = 0;
            }
            else
            {
                bi.Obstacle = true;
                // bi.Height = 14; // original blocking behaviour
            }

            using (new TerrainUpdateMonitor(zone))
            {
                var radius = (int) (ED.Config.item_work_range ?? 1);

                for (var y = -radius; y <= radius; y++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        var gx = CurrentPosition.intX + x;
                        var gy = CurrentPosition.intY + y;

                        zone.Terrain.Blocks[gx,gy] = bi;
                        zone.Terrain.Plants.UpdateValue(gx,gy,pi =>
                        {
                            pi.Clear();
                            return pi;
                        });
                    }
                }
            }
        }

        public void Rename(Character character, string name)
        {
            name.Length.ThrowIfGreater(32, ErrorCodes.NameTooLong);

            Corporation.GetCorporationEidAndRoleFromSql(character, out long corporationEid, out CorporationRole role);

            if (corporationEid != Owner)
                throw new PerpetuumException(ErrorCodes.AccessDenied);

            role.IsAnyRole(CorporationRole.CEO,CorporationRole.DeputyCEO,CorporationRole.editPBS).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

            var oldName = Name;
            Name = name;
            Save();

            Logger.Info(this + " got renamed from " + oldName + " to " + name);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);
            if(source is Player)
            {
                source.ApplyPvPEffect();
            }
        }

        public bool IsVisible(Character character)
        {
            Corporation.GetCorporationEidAndRoleFromSql(character, out long corporationEid, out CorporationRole role);
            if (Owner == corporationEid)
            {
                return role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.viewPBS);
            }
            return false;
        }
    }
}