using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.Zones.ProximityProbes
{
    public interface ICharactersRegistered
    {
        Character[] GetRegisteredCharacters();
        void ReloadRegistration();
        int GetMaxRegisteredCount();
    }

    //ez van kinn a terepen
    public abstract class ProximityProbeBase : Unit ,  ICharactersRegistered
    {
        private readonly CharactersRegisterHelper<ProximityProbeBase> _charactersRegisterHelper;
        private IntervalTimer _probingInterval = new IntervalTimer(TimeSpan.FromSeconds(10));
        private UnitDespawnHelper _despawnHelper;

        protected ProximityProbeBase()
        {
            _charactersRegisterHelper = new CharactersRegisterHelper<ProximityProbeBase>(this);
        }

        public ICorporationManager CorporationManager { get; set; }

        public virtual void CheckDeploymentAndThrow(IZone zone, Position spawnPosition)
        {
            zone.Units.OfType<DockingBase>().WithinRange(spawnPosition, DistanceConstants.PROXIMITY_PROBE_DEPLOY_RANGE_FROM_BASE).Any().ThrowIfTrue(ErrorCodes.NotDeployableNearObject);
            zone.Units.OfType<Teleport>().WithinRange(spawnPosition, DistanceConstants.PROXIMITY_PROBE_DEPLOY_RANGE_FROM_TELEPORT).Any().ThrowIfTrue(ErrorCodes.TeleportIsInRange);
        }

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
            _despawnHelper.DespawnStrategy = Kill;
        }

        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _probingInterval.Update(time);

            if (!_probingInterval.Passed)
                return;

            _probingInterval.Reset();

            if (IsActive)
            {
                //detect
                var robotsNearMe = GetNoticedUnits();

                //do something
                OnUnitsFound(robotsNearMe);
            }

            if (_despawnHelper == null)
            {
                var m = GetPropertyModifier(AggregateField.despawn_time);
                var timespan = TimeSpan.FromMilliseconds((int)m.Value);
                SetDespawnTime(timespan);
            }

            _despawnHelper.Update(time, this);
        }

        protected virtual bool IsActive => true;

        #region registration 

        //ezt kell hivogatni requestbol, ha valtozott
        public void ReloadRegistration()
        {
           _charactersRegisterHelper.ReloadRegistration();
        }

        public Character[] GetRegisteredCharacters()
        {
            return _charactersRegisterHelper.GetRegisteredCharacters();
        }

        public int GetMaxRegisteredCount()
        {
            return _charactersRegisterHelper.GetMaxRegisteredCount();
        }

        #endregion

        #region probe functions

        // egy adott pillanatban kiket lat
        [CanBeNull]
        public abstract List<Player> GetNoticedUnits();

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            _probingInterval = new IntervalTimer(GetProbeInterval(), true);
        }

        public virtual void OnProbeDead()
        {
            //uccso info a regisztraltaknak
            SendProbeDead();
            PBSRegisterHelper.ClearMembersFromSql(Eid);
            Zone.UnitService.RemoveUserUnit(this);
            Logger.Info("probe got deleted " + Eid);
        }
        
        public virtual void OnProbeCreated()
        {
            //elso info arrol hogy letrejott

            Logger.Info("probe created " + Eid);

            SendProbeCreated();
           
        }


        public virtual void OnUnitsFound(List<Player> unitsFound)
        {
            //itt lehet mindenfele, pl most kuldunk egy kommandot amire a kliens terkepet frissit

            if (unitsFound.Count <= 0) return;

            var registerdCharacters = GetRegisteredCharacters();

            if (registerdCharacters.Length <= 0) return;

            var infoDict = CreateInfoDictionaryForProximityProbe(unitsFound);

            Message.Builder.SetCommand(Commands.ProximityProbeInfo).WithData(infoDict).ToCharacters(registerdCharacters).Send();
        }

        #endregion

        protected override void OnDead(Unit killer)
        {
            OnProbeDead();
            base.OnDead(killer);
        }

        public Dictionary<string, object> GetProbeInfo(bool includeRegistered = true)
        {
            var info = BaseInfoToDictionary();

            var probeDict = new Dictionary<string, object>();

            if (includeRegistered)
            {
                probeDict.Add(k.registered, GetRegisteredCharacters().GetCharacterIDs().ToArray());
            }

            probeDict.Add(k.zoneID, Zone.Id);
            probeDict.Add(k.x, CurrentPosition.X);
            probeDict.Add(k.y, CurrentPosition.Y);
            info.Add("probe", probeDict);
            return info;
        }

        /// <summary>
        /// All info included
        /// </summary>
        /// <returns></returns>
        public override Dictionary<string, object> ToDictionary()
        {
            return GetProbeInfo();
        }

        public void SendProbeCreated()
        {
            var membersToInfom = GetAllPossibleMembersToInfom();
            Message.Builder.SetCommand(Commands.ProximityProbeCreated).WithData(ToDictionary()).ToCharacters(membersToInfom).Send();
        }

        public void SendUpdateToAllPossibleMembers()
        {
            var members = GetAllPossibleMembersToInfom();

            Message.Builder.SetCommand(Commands.ProximityProbeUpdate).WithData(ToDictionary()).ToCharacters(members).Send();
        }

        private void SendProbeDead()
        {
            var membersToInfom = GetAllPossibleMembersToInfom();

            Message.Builder.SetCommand(Commands.ProximityProbeDead).WithData(ToDictionary()).ToCharacters(membersToInfom).Send();
        }

        public IEnumerable<Character> GetAllPossibleMembersToInfom()
        {
            return GetRegisteredCharacters().Concat(GetProximityBoard(Owner)).Distinct();
        }

        private IEnumerable<Character> GetProximityBoard(long corporationEid)
        {
            const CorporationRole roleMask = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.Accountant;
            return CorporationManager.LoadCorporationMembersWithAnyRole(corporationEid,roleMask);
        }

        public Dictionary<string, object> CreateInfoDictionaryForProximityProbe(  List<Player> unitsFound)
        {
            var infoDict = GetProbeInfo(false);

            var unitsInfo = unitsFound.ToDictionary("c", p =>
            {
                return new Dictionary<string, object>
                {
                    {k.characterID, p.Character.Id},
                    {k.x, p.CurrentPosition.X}, 
                    {k.y, p.CurrentPosition.Y}
                };
            });

            infoDict.Add(k.units, unitsInfo);

            return infoDict;
        }

        public void InitProbe(IEnumerable<Character> summonerCharacters )
        {
            PBSRegisterHelper.WriteRegistersToDb(Eid, summonerCharacters);
            _probingInterval.Interval = TimeSpan.FromMilliseconds(GetProbeInterval());
        }

        public virtual int GetProbeInterval()
        {
            var config = EntityDefault.Get(Definition).Config;

            if (config.cycle_time == null)
            {
                Logger.Error("consistency error in proximityProbe. interval not defined. " + Definition + " " + ED.Name);
                return 150000;
            }

            return ((int) config.cycle_time) + FastRandom.NextInt(0,250);
        }

        public bool IsRegistered(Character character)
        {
            return GetRegisteredCharacters().Contains(character);
        }

        public ErrorCodes HasAccess(Character character)
        {
            if (IsRegistered(character))
            {
                return ErrorCodes.NoError;
            }

            var corporationEid = character.CorporationEid;

            if (corporationEid != Owner)
            {
                return ErrorCodes.AccessDenied;
            }

            var role = Corporation.GetRoleFromSql(character);

            if (IsAllProbesVisible(role))
            {
                return ErrorCodes.NoError;
            }

            return ErrorCodes.AccessDenied;
        }

        public Dictionary<string, object> GetProbeRegistrationInfo()
        {
            var ownerCorporation = Corporation.GetOrThrow(Owner);
            var maxRegistered = ownerCorporation.GetMaximumRegisteredProbesAmount();
            var currentRegistered = GetRegisteredCharacters().Length;
            var boardMembers = ownerCorporation.GetBoardMembersCount();

            var result = new Dictionary<string, object>
            {
                {k.eid, Eid },
                {"maxRegistered", maxRegistered},
                {"freeSlots", (maxRegistered - (currentRegistered - boardMembers).Clamp(0, int.MaxValue))},
                {"currentlyRegistered", currentRegistered},
                {"boardMembers", boardMembers},
            };

            return result;
        }

        public static bool IsAllProbesVisible(CorporationRole role)
        {
            return role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO);
        }
    }

}
