using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Units;
using Perpetuum.Zones.PBS.Connections;

namespace Perpetuum.Zones.PBS
{
    //ez aki kezel a connectionoket
    //felepulest, ativalast, szetszedest

    /// <summary>
    /// This class handles the connection, construction, reinforcement, etc. Base for all PBSObject
    /// </summary>
    public abstract class PBSObject : Unit, IPBSObject
    {
        private PBSObjectHelper<PBSObject> _pbsObjectHelper;
        private PBSReinforceHandler<PBSObject> _reinforceHandler;
        public int ZoneIdCached { get; private set; }

        IPBSReinforceHandler IPBSObject.ReinforceHandler => _reinforceHandler;

        public void SetReinforceHandler(PBSReinforceHandler<PBSObject> reinforceHandler)
        {
            _reinforceHandler = reinforceHandler;
        }

        public void SetPBSObjectHelper(PBSObjectHelper<PBSObject> helper)
        {
            _pbsObjectHelper = helper;
            _pbsObjectHelper.OnlineStatusChanged += OnOnlineStatusChanged;
            if (_pbsObjectHelper.ConnectionHandler is INotifyConnectionModified n)
            {
                n.ConnectionCreated += OnConnectionCreated;
                n.ConnectionDeleted += OnConnectionDeleted;
            }
        }

        protected virtual void OnConnectionCreated(PBSConnection pbsConnection)
        {
           
        }

        protected virtual void OnConnectionDeleted(PBSConnection pbsConnection)
        {

        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
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

            return info;
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            _reinforceHandler.AddToDictionary(info);
            _pbsObjectHelper.AddToDictionary(info);
            return info;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _pbsObjectHelper.Init();
            _reinforceHandler.Init();
            ZoneIdCached = zone.Id;
            base.OnEnterZone(zone, enterType);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            _pbsObjectHelper.RemoveFromZone(zone);
            this.SendNodeDead();
            base.OnRemovedFromZone(zone);
        }

        public override void OnInsertToDb()
        {
            _pbsObjectHelper.Init();
            base.OnInsertToDb();
        }

        private void SaveHelpersToDb()
        {
            _pbsObjectHelper.OnSave();
            _reinforceHandler.OnSave();
        }

        public override void OnUpdateToDb()
        {
            SaveHelpersToDb();
            base.OnUpdateToDb();
        }

        protected override void OnDead(Unit killer)
        {
            _pbsObjectHelper.DropLootToZone(Zone,this,killer);
            base.OnDead(killer);
        }

        public IPBSConnectionHandler ConnectionHandler => _pbsObjectHelper.ConnectionHandler;

        public ErrorCodes ModifyConstructionLevel(int amount, bool force = false)
        {
            return _pbsObjectHelper.ModifyConstructionLevel(amount, force);
        }

        public int ConstructionLevelMax => _pbsObjectHelper.ConstructionLevelMax;
        public int ConstructionLevelCurrent => _pbsObjectHelper.ConstructionLevelCurrent;

        protected override void DoExplosion()
        {
            //NO base call!!!
        }

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

        public event Action<Unit, bool> OrphanedStateChanged
        {
            add => _pbsObjectHelper.OrphanedStateChanged += value;
            remove => _pbsObjectHelper.OrphanedStateChanged -= value;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            //minden pbs node kell h csinalja, felepulest mentegeti pl stb
            _reinforceHandler.OnUpdate(time);
            _pbsObjectHelper.OnUpdate(time);
            
            base.OnUpdate(time);
        }
       
        protected virtual void OnOnlineStatusChanged(bool onlineStatus)
        {
        }

        public void SendNodeUpdate(PBSEventType eventType = PBSEventType.nodeUpdate)
        {
            _pbsObjectHelper.SendNodeUpdate(eventType);
        }
    }
}
