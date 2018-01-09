using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Units;
using Perpetuum.Zones.PBS.Connections;

namespace Perpetuum.Zones.PBS.HighwayNode
{
    public class PBSHighwayNode : PBSActiveObject, IPBSCorePump, IPBSUsesCore, IPBSAcceptsCore
    {
        private readonly CorePumpHandler<PBSHighwayNode> _corePumpHandler;
        private readonly CoreUseHandler<PBSHighwayNode> _coreUseHandler;

        public PBSHighwayNode()
        {
            _coreUseHandler = new CoreUseHandler<PBSHighwayNode>(this, new EnergyStateFactory(this));
            _corePumpHandler = new CorePumpHandler<PBSHighwayNode>(this);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }


        public ICorePumpHandler CorePumpHandler { get { return _corePumpHandler; } }

        public ICoreUseHandler CoreUseHandler { get { return _coreUseHandler; } }

        protected override void OnUpdate(TimeSpan time)
        {
            // fogyaszt coret
            _coreUseHandler.OnUpdate(time);
            base.OnUpdate(time);

        }

        private double _totalCoreUse;


        protected override void PBSActiveObjectAction(IZone zone)
        {

            if (_coreUseHandler.EnergyState == PBSEnergyState.active)
            {
                //pumpal ki coret ha kell
                _corePumpHandler.TransferToConnections();
            }

            //fogyasztas + kipumpalas 
            _totalCoreUse = _coreUseHandler.LastCoreUse + _corePumpHandler.LastUsedCore;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            _coreUseHandler.Init();
        }

        public bool TryCollectCoreConsumption(out double coreDemand)
        {
            var sumCore = 0.0;
            var consumption = this.GetCoreConsumption();
            
            foreach (var highwayNode in GetConnectedHighwayTargets())
            {
                if (highwayNode.IsGoodHighwayTarget())
                {
                    //%ban noveli a fogyasztast tavolsag alapjan. 20 csempere van akkor 20%al fogyaszt tobbet

                    var distanceMultiplier = 1 + CurrentPosition.TotalDistance2D(highwayNode.CurrentPosition) / 100;

                    var nodeCore = consumption*distanceMultiplier;

                    sumCore += nodeCore;
                }
            }

            coreDemand = sumCore;

            return true;
        }

        public bool IsGoodHighwayTarget()
        {
            return this.IsFullyConstructed() && OnlineStatus && _coreUseHandler.EnergyState == PBSEnergyState.active;
        }


        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            
            _corePumpHandler.AddToDictionary(info);
            _coreUseHandler.AddToDictionary(info);

            info[k.lastUsedCore] = _totalCoreUse;

            return info;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _corePumpHandler.AddToDictionary(info);
            _coreUseHandler.AddToDictionary(info);

            info[k.lastUsedCore] = _totalCoreUse;
            return info;
        }

        

        protected override void OnConnectionCreated(PBSConnection connection)
        {
            RefreshAll();
            base.OnConnectionCreated(connection);
        }

        protected override void OnConnectionDeleted(PBSConnection pbsConnection)
        {
            RefreshAll(pbsConnection.TargetPbsObject as PBSHighwayNode);
            
            base.OnConnectionDeleted(pbsConnection);
        }

        private List<PBSHighwayNode> GetConnectedHighwayTargets()
        {
            return ConnectionHandler.OutConnections
                .Where(c => c.TargetPbsObject is PBSHighwayNode)
                .Select(c => (PBSHighwayNode) c.TargetPbsObject)
                .ToList();
        }
        

        public List<HighwaySegmentInfo> GetOutgoingLiveSegments()
        {
            var mySegments = new List<HighwaySegmentInfo>();

            if (!IsGoodHighwayTarget())
            {
                return mySegments;
            }
            
            foreach (var connectedHighwayNode in GetConnectedHighwayTargets())
            {
                if (!connectedHighwayNode.IsGoodHighwayTarget())
                    continue;

                var i = new HighwaySegmentInfo()
                {
                    StatPosition = CurrentPosition,
                    EndPosition = connectedHighwayNode.CurrentPosition,
                    Radius = Math.Max( WorkRange, connectedHighwayNode.WorkRange)
                };

                mySegments.Add(i);

            }

            return mySegments;
        }

        public int WorkRange
        {
            get
            {
                if (ED.Config.item_work_range != null)
                    return (int) ED.Config.item_work_range;

                Logger.Error("no item_work_range is defined for " + ED.Name + " " + ED.Definition);
                return 10;
            }

        }

        protected override void OnDead(Unit killer)
        {
            RefreshAll();
            base.OnDead(killer);
        }


        protected override void OnOnlineStatusChanged(bool onlineStatus)
        {
            if (_coreUseHandler.EnergyState != PBSEnergyState.inactive)
            {
                RefreshAll();    
            }
            
            base.OnOnlineStatusChanged(onlineStatus);
        }

        private void RefreshAll(PBSHighwayNode extraNode = null)
        {
            var relatedAreas =
                ConnectionHandler.InConnections
                    .Concat(ConnectionHandler.OutConnections)
                    .Select(c => c.TargetPbsObject).OfType<PBSHighwayNode>()
                    .Select(n => new HighwaySegmentInfo
                    {
                        EndPosition = n.CurrentPosition,
                        Radius = Math.Max(WorkRange, n.WorkRange),
                        StatPosition = CurrentPosition
                    })
                    .Select(si => si.BoundingArea())
                    .ToList();


            if (extraNode != null)
            {

                var s = new HighwaySegmentInfo
                {
                    EndPosition = extraNode.CurrentPosition,
                    Radius = Math.Max(WorkRange, extraNode.WorkRange),
                    StatPosition = CurrentPosition
                };

                var area = s.BoundingArea();
                relatedAreas.Add(area);
            }

            Zone.HighwayHandler.SubmitMore(relatedAreas);
        }


        private class EnergyStateFactory : IEnergyStateFactory<PBSHighwayNode>
        {
            private readonly PBSHighwayNode _node;

            public EnergyStateFactory(PBSHighwayNode node)
            {
                _node = node;
            }

            public WarmUpRawCoreState<PBSHighwayNode> CreateWarmUpEnergyState()
            {
                return new WarmUpEnergyState(_node);
            }

            public ActiveRawCoreState<PBSHighwayNode> CreateActiveEnergyState()
            {
                return new ActiveEnergyState(_node);
            }
        }

        private class ActiveEnergyState : ActiveCoreUserNodeState<PBSHighwayNode>
        {
            public ActiveEnergyState(PBSHighwayNode owner) : base(owner)
            {
            }

            public override void Enter()
            {
                Owner.RefreshAll();
                base.Enter();
            }
        }
      

        private class WarmUpEnergyState : WarmUpCoreUserNodeState<PBSHighwayNode>
        {
            public WarmUpEnergyState(PBSHighwayNode owner) : base(owner)
            {
            }

            public override void Enter()
            {
                Owner.RefreshAll();
                base.Enter();
            }
        }

    }


}

