using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.PBS.ProductionNodes
{
    /// <summary>
    /// This node can be connected to a facility node to increase its efficiency
    /// </summary>
    public class PBSFacilityUpgradeNode : PBSActiveObject,  IPBSAcceptsCore, IPBSUsesCore
    {
        private readonly CoreUseHandler<PBSFacilityUpgradeNode> _coreUseHandler;

        public PBSFacilityUpgradeNode()
        {
            _coreUseHandler = new CoreUseHandler<PBSFacilityUpgradeNode>(this,new EnergyStateFactory(this));
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public ICoreUseHandler CoreUseHandler { get { return _coreUseHandler; } }
       

        public bool TryCollectCoreConsumption(out double coreDemand)
        {
            //van-e olyan facility nodera k0tve ami be van kotve bazisra
            var anyOperatingConnectedObject = ConnectionHandler.OutConnections
                .Select(c => c.TargetPbsObject)
                .OfType<PBSProductionFacilityNode>()
                .Any(f => f.IsWorking);

            coreDemand = 0;
            return !anyOperatingConnectedObject; //false: use the config value

        }

        
        

        protected override void OnUpdate(TimeSpan time)
        {
            _coreUseHandler.OnUpdate(time);
            base.OnUpdate(time);
        }


        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _coreUseHandler.Init();
            
            base.OnEnterZone(zone, enterType);
        }

      

       

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            
            _coreUseHandler.AddToDictionary(info);
            return info;
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.ToDictionary();
            
            _coreUseHandler.AddToDictionary(info);
            return info;

        }


       

        private int _levelIncrease;
        public int GetLevelIncrease()
        {
           return  PBSHelper.LazyInitProductionLevelIncrease(this, ref _levelIncrease );
        }

        

        public bool IsContributing()
        {
            if (!OnlineStatus) return false;

            if (_coreUseHandler.EnergyState == PBSEnergyState.active)
            {
                return true;    
            }

            return false;

        }

        private class EnergyStateFactory : IEnergyStateFactory<PBSFacilityUpgradeNode>
        {
            private readonly PBSFacilityUpgradeNode _node;

            public EnergyStateFactory(PBSFacilityUpgradeNode node)
            {
                _node = node;
            }

            public WarmUpRawCoreState<PBSFacilityUpgradeNode> CreateWarmUpEnergyState()
            {
                return new WarmUpenergyState(_node);
            }

            public ActiveRawCoreState<PBSFacilityUpgradeNode> CreateActiveEnergyState()
            {
                return new ActiveEnergyState(_node);
            }
        }

        private class WarmUpenergyState : WarmUpCoreUserNodeState<PBSFacilityUpgradeNode>
        {
            public WarmUpenergyState(PBSFacilityUpgradeNode owner) : base(owner)
            {
            }
        }

        private class ActiveEnergyState : ActiveCoreUserNodeState<PBSFacilityUpgradeNode>
        {
            public ActiveEnergyState(PBSFacilityUpgradeNode owner) : base(owner)
            {
            }
        }
        
    }

    
    


}
