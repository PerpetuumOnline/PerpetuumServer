using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Log;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.PBS.Connections;

namespace Perpetuum.Zones.PBS.EffectNodes
{
    //ez tol ki effektet / frissiti oket de csak a connected nodeokra

    /// <summary>
    /// Applies an effect to the connected nodes
    /// </summary>
    public class PBSEffectSupplier : PBSEffectNode
    {
        private readonly StackFSM _fsm= new StackFSM();

        private static void WriteLog(string message)
        {
            Logger.Info(message);
        }


        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            
            _fsm.Push(new EmitState(this));
        }


        protected override IEnumerable<Unit> GetTargetUnits()
        {
            var targetUnits = ConnectionHandler.OutConnections
               .Select(c => c.TargetPbsObject)
               .OfType<Unit>();

            return targetUnits;
        }

        protected override double CollectCoreConsumption()
        {
            var coreDemand = this.GetCoreConsumption() * GetTargetUnits().Count();

            return coreDemand;
        }


        private bool _needsCleanUp;

        protected override void OnConnectionDeleted(PBSConnection pbsConnection)
        {
            _needsCleanUp = true;
            WriteLog("clean up state push requested " + ED.Name + " " + Eid);
            base.OnConnectionDeleted(pbsConnection);
        }
        

        protected override void OnUpdate(TimeSpan time)
        {
            _fsm.Update(time);
            base.OnUpdate(time);
        }


        private class EmitState : IState
        {
            private readonly PBSEffectSupplier _supplier;

            public EmitState(PBSEffectSupplier supplier)
            {
                _supplier = supplier;
            }


            public void Enter()
            {
                WriteLog("enter: emit state " + _supplier.ED.Name + " " + _supplier.Eid);
                _supplier.ApplyCurrentEffect();
            }

            public void Exit()
            {
                
            }

            public void Update(TimeSpan time)
            {
                if (_supplier._needsCleanUp)
                {
                    WriteLog("cleanup state push " + _supplier.ED.Name + " " + _supplier.Eid);
                    _supplier._fsm.Push(new CleanUpState(_supplier));
                    
                }
            }
        }

        private class CleanUpState : IState
        {
            private readonly PBSEffectSupplier _supplier;
            private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(20));

            public CleanUpState(PBSEffectSupplier supplier)
            {
                _supplier = supplier;
            }

            public void Enter()
            {
                WriteLog("enter: clean up state " + _supplier.ED.Name + " " + _supplier.Eid);
                _supplier.RemoveCurrentEffect();
                _supplier._needsCleanUp = false;
            }

            public void Exit()
            {
                _supplier._needsCleanUp = false;
            }

            public void Update(TimeSpan time)
            {
                _timer.Update(time);
                if (!_timer.Passed) return;

                //20 masodperc mulva visszavalt emitbe
                _supplier._fsm.Pop();

                WriteLog("clean up state popped " + _supplier.ED.Name + " " + _supplier.Eid);
            }
        }

    }
    
}
