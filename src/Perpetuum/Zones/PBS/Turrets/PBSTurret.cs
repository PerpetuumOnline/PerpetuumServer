using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.PBS.Connections;

namespace Perpetuum.Zones.PBS.Turrets
{
    /// <summary>
    /// Buildable defensive turret
    /// </summary>
    public class PBSTurret : Turret, IPBSObject, IPBSAcceptsCore, IStandingController, IBlobableUnit,IBlobEmitter, IPBSUsesCore,IPBSEventHandler
    {
        private readonly IStandingHandler _standingHandler;
        private readonly BlobHandler<PBSTurret> _blobHandler;
        private readonly IBlobEmitter _blobEmitter;
        private readonly PBSObjectHelper<PBSTurret> _pbsObjectHelper;
        private readonly PBSStandingController<PBSTurret> _standingController;
        private readonly PBSReinforceHandler<PBSTurret> _reinforceHandler;
        private readonly CoreUseHandler<PBSTurret> _coreUseHandler;

        public PBSTurret(IStandingHandler standingHandler,PBSObjectHelper<PBSTurret>.Factory pbsObjectHelperFactory)
        {
            _standingHandler = standingHandler;
            _pbsObjectHelper = pbsObjectHelperFactory(this);
            _standingController = new PBSStandingController<PBSTurret>(this) { AlwaysEnabled = true };
            
            _blobHandler = new BlobHandler<PBSTurret>(this);
            _blobEmitter = new BlobEmitter(this);
            _reinforceHandler = new PBSReinforceHandler<PBSTurret>(this);
            _coreUseHandler = new CoreUseHandler<PBSTurret>(this, new EnergyStateFactory(this));

        }

        private  double GetCoreMinimumFromModules()
        {
            var maxCoreUsage = ActiveModules.Max(m => m.CoreUsage);
            Logger.Info(ED.Name + " max module core usage " + maxCoreUsage );

            return maxCoreUsage;
        }

        public int ZoneIdCached { get; private set; }

        public IBlobHandler BlobHandler => _blobHandler;

        public IPBSReinforceHandler ReinforceHandler => _reinforceHandler;
        public IPBSConnectionHandler ConnectionHandler => _pbsObjectHelper.ConnectionHandler;

        public ErrorCodes ModifyConstructionLevel(int amount, bool force = false)
        {
            return _pbsObjectHelper.ModifyConstructionLevel(amount, force);
        }

        public int ConstructionLevelMax => _pbsObjectHelper.ConstructionLevelMax;
        public int ConstructionLevelCurrent => _pbsObjectHelper.ConstructionLevelCurrent;
        public void SetOnlineStatus(bool state, bool checkNofBase, bool forcedByServer = false)
        {
            _pbsObjectHelper.SetOnlineStatus(state,checkNofBase,forcedByServer);
        }

        public bool OnlineStatus => _pbsObjectHelper.OnlineStatus;
        public void TakeOver(long newOwner)
        {
            _pbsObjectHelper.TakeOver(newOwner);
        }

        public bool IsOrphaned
        {
            get => _pbsObjectHelper.IsOrphaned;
            set => _pbsObjectHelper.IsOrphaned = value;
        }

        public event Action<Unit,bool> OrphanedStateChanged
        {
            add => _pbsObjectHelper.OrphanedStateChanged += value;
            remove => _pbsObjectHelper.OrphanedStateChanged -= value;
        }

        public void SendNodeUpdate(PBSEventType eventType)
        {
            _pbsObjectHelper.SendNodeUpdate(eventType);
        }

        public ICoreUseHandler CoreUseHandler => _coreUseHandler;

        public override void OnUpdateToDb()
        {
            _pbsObjectHelper.OnSave();
            _reinforceHandler.OnSave();
            base.OnUpdateToDb();
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _coreUseHandler.Init();

            _coreUseHandler.CoreMinimum = GetCoreMinimumFromModules();

            _pbsObjectHelper.Init();
            _reinforceHandler.Init();

            AI.ToInactiveAI();

            ZoneIdCached = zone.Id;

            base.OnEnterZone(zone, enterType);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            _pbsObjectHelper.RemoveFromZone(zone);
            this.SendNodeDead();
            base.OnRemovedFromZone(zone);
        }


        protected override void OnDead(Unit killer)
        {
            var zone = Zone;
            _pbsObjectHelper.DropLootToZone(zone, this, killer);
            base.OnDead(killer);
        }

        public override ErrorCodes IsAttackable
        {
            get
            {
                var err = base.IsAttackable;
                if (err == ErrorCodes.NoError)
                {
                    if (_reinforceHandler.CurrentState.IsReinforced)
                        err = ErrorCodes.TargetIsNonAttackable_Reinforced;

                }
                return err;
            } 
        }


        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            _pbsObjectHelper.AddToDictionary(info);
            _reinforceHandler.AddToDictionary(info);
            _coreUseHandler.AddToDictionary(info);

            //tok jo lenne ha meg lehetne mondani, hogy mennyi coret hasznalt el az elmult 30 secben
            info.Remove(k.lastUsedCore);

            return info;
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();

            
            _pbsObjectHelper.AddToDictionary(info);
            _reinforceHandler.AddToDictionary(info);
            _coreUseHandler.AddToDictionary(info);

            info.Remove(k.lastUsedCore);

            return info;
        }

        protected override void DoExplosion()
        {
            //NO base call!!!
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public double BlobEmission
        {
            get { return _blobEmitter.BlobEmission; }
        }

        public double BlobEmissionRadius
        {
            get { return _blobEmitter.BlobEmissionRadius; }
        }

        internal override bool IsHostile(Player player)
        {
            return IsHostileCorporation(player.CorporationEid);
        }

        internal override bool IsHostile(Gate gate)
        {
            return IsHostileCorporation(gate.Owner);
        }

        private bool IsHostileCorporation(long corporationEid)
        {
            if (corporationEid == Owner)
                return false;

            if (DefaultCorporationDataCache.IsCorporationDefault(corporationEid))
                return true;

            var standing = _standingHandler.GetStanding(Owner, corporationEid);
            return StandingLimit > standing;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _coreUseHandler.OnUpdate(time);

            base.OnUpdate(time);

            _blobHandler.Update(time);
            _pbsObjectHelper.OnUpdate(time);
            _reinforceHandler.OnUpdate(time);
        }
       
        public bool TryCollectCoreConsumption(out double coreDemand)
        {
            coreDemand = 0;
            return false;//fallback to config
        }

        private class EnergyStateFactory : IEnergyStateFactory<PBSTurret>
        {
            private readonly PBSTurret _turret;

            public EnergyStateFactory(PBSTurret turret)
            {
                _turret = turret;
            }

            public WarmUpRawCoreState<PBSTurret> CreateWarmUpEnergyState()
            {
                return new WarmUpCoreState(_turret);
            }

            public ActiveRawCoreState<PBSTurret> CreateActiveEnergyState()
            {
                return new ActiveCoreState(_turret);
            }
        }

        private class WarmUpCoreState : WarmUpCoreUserNodeState<PBSTurret>
        {
            public WarmUpCoreState(PBSTurret owner) : base(owner)
            {
            }

            public override void Enter()
            {
                Owner.AI.ToInactiveAI();
                base.Enter();
            }
        }

        private class ActiveCoreState : ActiveCoreUserNodeState<PBSTurret>
        {
            public ActiveCoreState(PBSTurret owner) : base(owner)
            {
            }

            public override void Enter()
            {
                // aktivba
                Owner.AI.ToActiveAI();
                base.Enter();
            }

            
            protected override void OnUpdate(TimeSpan time)
            {
                base.OnUpdate(time);

                //ez az extra energy state check azert kell, hogy ne legyen sorrendfuggo az update
                //ha kivennenk akkor amikor atmegy warmupba akkor meg egyszer meghivja az active state updatejet es az visszakapcsolja az AIt
                if (Owner.IsFullyConstructed() && Owner.OnlineStatus && Owner.CoreUseHandler.EnergyState == PBSEnergyState.active )
                {
                    Owner.AI.ToActiveAI();
                }
                else
                {
                    Owner.AI.ToInactiveAI();
                }
            }
        }

        public void HandlePBSEvent(IPBSObject sender, PBSEventArgs e)
        {
            if (!OnlineStatus || !this.IsFullyConstructed())
                return;

            var attacked = e as NodeAttackedEventArgs;
            if (attacked == null)
                return;

            var attacker = Zone.ToPlayerOrGetOwnerPlayer(attacked.Attacker) ?? attacked.Attacker;

            // valaki megtamadott valamit
            LockHostile(attacker, true);
        }

        public double StandingLimit
        {
            get { return _standingController.StandingLimit; }
            set { _standingController.StandingLimit = value; }
        }

        public bool StandingEnabled
        {
            get { return _standingController.Enabled; }
            set { _standingController.Enabled = value; }
        }
    }
}
