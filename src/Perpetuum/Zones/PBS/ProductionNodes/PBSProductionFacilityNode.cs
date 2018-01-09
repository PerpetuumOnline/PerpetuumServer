using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS.ProductionNodes
{
    //ez kuldozget requestet a production engine-nek eletben tartja a production facility-t

    /// <summary>
    /// This class updates the connected docking base and enables the production facilities
    /// </summary>
    public abstract class PBSProductionFacilityNode : PBSActiveObject,  IPBSUsesCore, IPBSAcceptsCore
    {
        private readonly CoreUseHandler<PBSProductionFacilityNode> _coreUseHandler;
        private PBSProductionFacilityNodeHelper _productionFacilityNodeHelper;

        public ProductionManager ProductionManager { get; set; }

        protected PBSProductionFacilityNode()
        {
            _coreUseHandler = new CoreUseHandler<PBSProductionFacilityNode>(this,new EnergyStateFactory(this));
        }

        public void SetProductionFacilityNodeHelper(PBSProductionFacilityNodeHelper helper)
        {
            _productionFacilityNodeHelper = helper;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public ICoreUseHandler CoreUseHandler => _coreUseHandler;

        protected override void OnUpdate(TimeSpan time)
        {
            _coreUseHandler.OnUpdate(time);
            base.OnUpdate(time);
        }

        public bool TryCollectCoreConsumption(out double coreDemand)
        {
            coreDemand = 0;
            if (GetConnectedDockingBase() == null)
            {
                return true; //nincs bekotve bazis, akkor ezt fogja hasznalni 0->nem von le, nem frissit
            }



            //use fix amount of core from config
            
            return false; //uses config
        }


       

       

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            _productionFacilityNodeHelper.AddToDictionary(info);
            
            _coreUseHandler.AddToDictionary(info);
            return info;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _productionFacilityNodeHelper.AddToDictionary(info);
            
            _coreUseHandler.AddToDictionary(info);
            return info;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _coreUseHandler.Init();
            
            _productionFacilityNodeHelper.Init(); 
            base.OnEnterZone(zone, enterType);
        }

        public override void OnUpdateToDb()
        {
            _productionFacilityNodeHelper.OnSave();
            base.OnUpdateToDb();
        }

        //ez mire is valo?
        public double PreLevel { get; set; }

        public bool IsWorking {
            get
            {
                return this.IsFullyConstructed()
                       && OnlineStatus
                       && GetConnectedDockingBase() != null;
            } 
        }

       

        public abstract ProductionFacilityType GetFacilityType();

        private int _productionLevelBase;
        public virtual int GetFacilityLevelBase()
        {
            return PBSHelper.LazyInitProductionLevelBase(this, ref _productionLevelBase);
        }

        protected virtual int CollectProductionLevel()
        {
            return PBSHelper.CollectFacilityNodeLevelFromInComingConnections(this);
        }

        protected override void OnConnectionDeleted(PBSConnection pbsConnection)
        {
            var pbsBase = pbsConnection.TargetPbsObject as PBSDockingBase;

            if (pbsBase != null)
            {
                //we just got disconnected from this docking base    
                DisableFacility(pbsBase, false);
            }

            base.OnConnectionDeleted(pbsConnection);
        }


        protected override void OnOnlineStatusChanged(bool onlineStatus)
        {
            if (!OnlineStatus)
            {
                var pbsBase = GetConnectedDockingBase();

                if (pbsBase != null)
                {
                    DisableFacility(pbsBase);
                }
               
            }
        }

        private void DisableFacility(PBSDockingBase pbsDockingBase, bool isConnected = true)
        {
            if (pbsDockingBase != null)
            {
                RefreshInfo(pbsDockingBase.Eid, 0, false, isConnected);
            }
        }

        [CanBeNull]
        private PBSDockingBase GetConnectedDockingBase()
        {
            var outConnection = ConnectionHandler.OutConnections.FirstOrDefault();
            return outConnection?.TargetPbsObject as PBSDockingBase;
           
        }


        private class  WarmupEnergyState : WarmUpCoreUserNodeState<PBSProductionFacilityNode>
        {
            public WarmupEnergyState(PBSProductionFacilityNode owner) : base(owner)
            {
            }

            public override void Enter()
            {
                base.Enter();


                //turn facility off
                var dockingBase = Owner.GetConnectedDockingBase();
                if (dockingBase == null) return;
                Owner.RefreshInfo(dockingBase.Eid, 0, false, true);
            }

          
        }

        private class ActiveEnergyState : ActiveCoreUserNodeState<PBSProductionFacilityNode>
        {
            public ActiveEnergyState(PBSProductionFacilityNode owner) : base(owner)
            {
            }

            protected override void PostCoreSubtract()
            {
                var dockingBase = Owner.GetConnectedDockingBase();
                if (dockingBase == null) return;

                //bamulatos grafika
                Owner.Zone.CreateBeam(BeamType.medium_laser, b => b.WithSource(Owner)
                    .WithTarget(dockingBase)
                    .WithState(BeamState.Hit)
                    .WithBulletTime(60)
                    .WithDuration(1337));

                //collect production level
                var currentLevel = Owner.CollectProductionLevel(); 

                //turn facility on
                Owner.RefreshInfo(dockingBase.Eid, currentLevel, true, true);

            }
        }

        private class EnergyStateFactory : IEnergyStateFactory<PBSProductionFacilityNode>
        {
            private readonly PBSProductionFacilityNode _node;

            public EnergyStateFactory(PBSProductionFacilityNode node)
            {
                _node = node;
            }

            public WarmUpRawCoreState<PBSProductionFacilityNode> CreateWarmUpEnergyState()
            {
                return new WarmupEnergyState(_node);
            }

            public ActiveRawCoreState<PBSProductionFacilityNode> CreateActiveEnergyState()
            {
                return new ActiveEnergyState(_node);
            }
        }

        private void RefreshInfo(long dockingBaseEid, int level,bool enable, bool isConnected)
        {
            Task.Run(() =>
            {
                var info = new ProductionRefreshInfo
                {
                    facilityType = GetFacilityType(),
                    level = level,
                    senderPBSEid = Eid,
                    targetPBSBaseEid = dockingBaseEid,
                    enable = enable,
                    isConnected = isConnected,
                };
                ProductionManager.RefreshPBSFacility(info);
            });
        }

        public void SendMessageToProductionEngineOnConnection(PBSDockingBase dockingBase)
        {
            Task.Run(() =>
            {
                var data = new ProductionRefreshInfo
                {
                    facilityType = GetFacilityType(),
                    level = 0,
                    senderPBSEid = Eid,
                    targetPBSBaseEid = dockingBase.Eid,
                    enable = false,
                    isConnected = true, //itt csak ez szamit, a tobbi default
                };

                ProductionManager.PBSFacilityConnected(data);
            });
        }
    }

}
