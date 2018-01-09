using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Units;
using Perpetuum.Zones.PBS.CoreTransmitters;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.HighwayNode;
using Perpetuum.Zones.PBS.ProductionNodes;

namespace Perpetuum.Zones.PBS.Connections
{
    public class PBSConnectionValidator : IEntityVisitor<PBSProductionFacilityNode>,IEntityVisitor<PBSCoreTransmitter>,IEntityVisitor<PBSHighwayNode>,IEntityVisitor<PBSFacilityUpgradeNode>
    {
        private readonly IPBSObject _target;
        public ErrorCodes Error { get; private set; }


        public PBSConnectionValidator(IPBSObject target)
        {
            _target = target;
            Error = ErrorCodes.NoError;
        }

        public void Visit(PBSProductionFacilityNode node)
        {
            //csak dockingbase-re lehet kotni

            if (!CheckTargetType<PBSDockingBase>())
                return;

            var dockingBaseInComingConnections = _target.ConnectionHandler.InConnections;

            //van-e mar ijen facility felkotve?

            foreach (var connection in dockingBaseInComingConnections)
            {
                if (node.GetType() != connection.TargetPbsObject.GetType()) continue;
                Error = ErrorCodes.FacilityTypeAlreadyConnected;
                return;
            }
        }

        public void Visit(PBSCoreTransmitter entity)
        {
            CheckTargetType<IPBSAcceptsCore>();
        }

        public void Visit(PBSHighwayNode entity)
        {
            CheckTargetType<PBSHighwayNode>();
        }

        public void Visit(PBSFacilityUpgradeNode entity)
        {
            CheckTargetType<PBSProductionFacilityNode>();
        }

        private bool CheckTargetType<T>() where T : class
        {
            if ((_target as T) == null)
            {
                Error = ErrorCodes.TargetIsIncompatible;
                return false;
            }

            return true;
        }

        public static ErrorCodes Validate<T>(T source, IPBSObject target) where T : Unit, IPBSObject
        {
            var validator = new PBSConnectionValidator(target);
            source.AcceptVisitor(validator);
            return validator.Error;
        }

    }




    public interface INotifyConnectionModified
    {
        event Action<PBSConnection> ConnectionCreated;
        event Action<PBSConnection> ConnectionDeleted;
    }


    public interface IPBSConnectionHandler
    {
        void CheckNewConnection(IPBSObject targetNode, bool b);
        List<IPBSObject> NetworkNodes { get; }
        void AttachConnection(PBSConnection inConnection);
        void RemoveConnectedObject(IPBSObject pbsObject);
        void CollectNodes(IDictionary<long, IPBSObject> collected);
        List<PBSConnection> OutConnections { get; }
        List<PBSConnection> InConnections { get; }
        void MakeConnection(IPBSObject targetNode, Character character); //itt a requestnel ellenorizz, es akkor lehet h nem kell ide a karakter
        void BreakConnection(IPBSObject targetNode, Character character);
        void SetWeight(IPBSObject targetNode, double weight);
        PBSConnection[] GetConnections();
        void RemoveAllConnections();
        IDictionary<string, object> ToDictionary();

        void SendEventToNetwork(PBSEventArgs pbsEventArgs);
    }

    /// <summary>
    /// Handles the connections of a pbs pbsObject
    /// </summary>
    public class PBSConnectionHandler<T> : IPBSConnectionHandler, INotifyConnectionModified where T : Unit,IPBSObject
    {

        private PBSConnection[] _connections;

        private readonly T _pbsObject;

        public PBSConnectionHandler(T pbsObject)
        {
            _pbsObject = pbsObject;
        }

        [Conditional("DEBUG")]
        private void WriteLog(string message)
        {
            Logger.Info(message);
        }


        public PBSConnection[] GetConnections()
        {
            return LazyInitializer.EnsureInitialized(ref _connections, ReadConnectionsFromSql);

        }

        private PBSConnection[] ReadConnectionsFromSql()
        {
            var connections = PBSHelper.LoadConnectionsFromSql(_pbsObject.Zone, _pbsObject.Eid).ToArray();
            WriteLog("sql read connections " + _pbsObject.ED.Name  + " " + connections.Length);
            return connections;
        }

      


        private const int CPU_POWER_IN_NETWORK_WITHOUT_BASE = 1;

        /// <summary>
        /// Make connection by request
        /// </summary>
        /// <param name="targetNode"></param>
        /// <param name="character"></param>
        public void MakeConnection(IPBSObject targetNode, Character character)
        {
            //source pbsObject access check
            _pbsObject.CheckAccessAndThrowIfFailed(character);
            
            //--- transmit radius check
            targetNode.ThrowIfEqual(_pbsObject, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var targetUnit = ((Unit)targetNode);
            targetUnit.CurrentPosition.IsInRangeOf2D(_pbsObject.CurrentPosition, _pbsObject.GetTransmitRadius()).ThrowIfFalse(ErrorCodes.TargetOutOfTransmitRadius);


            //check pair--------------------------
            _pbsObject.ConnectionHandler.CheckNewConnection(targetNode, true);
            targetNode.ConnectionHandler.CheckNewConnection(_pbsObject, false);


            //-------check networks and owners
            var changeOwner = false;
            long newOwner = 0L;
         
            var sourceNetwork = NetworkNodes.ToArray();
            
            IPBSObject[] targetNetwork = null;
            var dockingBaseInSourceNetwork = false;
            var dockingBaseInTargetNetwork = false;

            var isSameNetwork = sourceNetwork.Any(p => p.GetHashCode() == targetNode.GetHashCode());
            
            if (!isSameNetwork)
            {
                //they are in different networks

                targetNetwork = targetNode.ConnectionHandler.NetworkNodes.ToArray();

                var sourceDockingBase = sourceNetwork.FirstOrDefault(n => n is PBSDockingBase) as PBSDockingBase;
                var targetDockingBase = targetNetwork.FirstOrDefault(n => n is PBSDockingBase) as PBSDockingBase;

                dockingBaseInSourceNetwork = sourceDockingBase != null;
                dockingBaseInTargetNetwork = targetDockingBase != null;

                //check nodes in networks
                if (dockingBaseInSourceNetwork)
                {
                    IsAnyNodeOutsideOfNetworkRange(sourceNetwork, sourceDockingBase).ThrowIfTrue(ErrorCodes.SomeNodeAreOutsideOfNetworkRange);
                    IsAnyNodeOutsideOfNetworkRange(targetNetwork, sourceDockingBase).ThrowIfTrue(ErrorCodes.SomeNodeAreOutsideOfNetworkRange);
                }

                var sumUsage = sourceNetwork.Concat(targetNetwork).Where(n => !(n is PBSDockingBase)).Sum(n => n.GetBandwidthUsage());

                //both networks have docking bases -> nono
                (dockingBaseInSourceNetwork && dockingBaseInTargetNetwork).ThrowIfTrue(ErrorCodes.BasesInBothNetworks);

                if (dockingBaseInSourceNetwork || dockingBaseInTargetNetwork)
                {
                    //there must be a docking base in one of the two
                    changeOwner = true;

                    //-------------------------------------- number of nodes check
                    int bandwidthCapacity;
                    if (dockingBaseInSourceNetwork)
                    {
                        bandwidthCapacity = sourceDockingBase.GetBandwidthCapacity;
                    }
                    else
                    {
                        bandwidthCapacity = targetDockingBase.GetBandwidthCapacity;
                    }

                    sumUsage.ThrowIfGreater(bandwidthCapacity, ErrorCodes.TooManyNodesOutOfCpu);

                    //------------------------------------------ new owner select
                    if (dockingBaseInSourceNetwork)
                    {
                        newOwner = _pbsObject.Owner;
                    }
                    else
                    {
                        newOwner = targetUnit.Owner; 
                    }

                    
                }
                else
                {
                    //no docking base

                    sumUsage.ThrowIfGreater(CPU_POWER_IN_NETWORK_WITHOUT_BASE, ErrorCodes.TooManyNodesOutOfCpu);

                    _pbsObject.Owner.ThrowIfNotEqual(targetUnit.Owner, ErrorCodes.OwnerMismatch);

                }
            }






            if (changeOwner)
            {
                if (newOwner == 0)
                {
                    //lekezeletlen resz %%%
                    Debug.Assert(false, " nincs megirva");
                }
                else
                {
                    if (dockingBaseInSourceNetwork)
                    {

                        foreach (var n in targetNetwork)
                        {
                            n.TakeOver(newOwner);
                        }
                    }

                    if (dockingBaseInTargetNetwork)
                    {
                        foreach (var n in sourceNetwork)
                        {
                            n.TakeOver(newOwner);
                        }

                    }
                }
            }

            
            //-----------------------------do actual work

            var outConnection = new PBSConnection(targetNode, _pbsObject, true);
            AttachConnection(outConnection);

            var inConnection = new PBSConnection(_pbsObject, targetNode, false);
            targetNode.ConnectionHandler.AttachConnection(inConnection);

            Transaction.Current.OnCommited(() =>
            {
                var pbsBase = NetworkNodes.FirstOrDefault(o => o is PBSDockingBase);
                if (pbsBase == null)
                    return;

                _pbsObject.ReinforceHandler.ForceDailyOffset(pbsBase.ReinforceHandler.ReinforceOffsetHours); //this node

                foreach (var node in NetworkNodes)
                {
                    if (node.Equals(_pbsObject)) continue;
                    if (node.Equals(pbsBase)) continue;
                    
                    //all reinforcable nodes in the network
                    node.ReinforceHandler.ForceDailyOffset(pbsBase.ReinforceHandler.ReinforceOffsetHours);
                }


            });
        }


        private bool IsAnyNodeOutsideOfNetworkRange(IEnumerable<IPBSObject> pbsObjects, PBSDockingBase pbsDockingBase)
        {
            foreach (var pbsObject in pbsObjects)
            {
                var unit = (Unit) pbsObject;

                if (PBSHelper.IsPlaceableOutsideOfBase((unit.ED.CategoryFlags)))
                {
                    continue;
                }

                if (!unit.CurrentPosition.IsInRangeOf2D(pbsDockingBase.CurrentPosition, pbsDockingBase.GetNetworkNodeRange()))
                {
                    return true;
                }
            }

            return false;
        }


        //server, non request
        public void RemoveAllConnections()
        {
            var deletedConnections = new List<PBSConnection>();
            var myNetwork = NetworkNodes;

            var connections = GetConnections();
            foreach (var pbsConnection in connections)
            {
                DeleteConnectionFromDb(pbsConnection);

                if (pbsConnection.IsOutgoing)
                    pbsConnection.TargetPbsObject.ConnectionHandler.RemoveConnectedObject(_pbsObject);
                
                deletedConnections.Add(pbsConnection);
            }

            Transaction.Current.OnCommited(() =>
            {
                Logger.Info("all connections deleted for " + _pbsObject.GetType() + "  " + _pbsObject.Eid + " deleted " + deletedConnections.Count + " connections.");
                foreach (var deletedConnection in deletedConnections)
                {
                    OnConnectionDeleted(deletedConnection);
                }

                //detect orphanship -- after deleting the connection set orphan status on all pbsObject 
                foreach (var pbsObject in myNetwork)
                {
                    if (pbsObject.Equals(_pbsObject)) continue; //not myself

                    var tmpNetwork = pbsObject.ConnectionHandler.NetworkNodes;
                    var orphanStatus = !tmpNetwork.Any(n => n is PBSDockingBase);
                    pbsObject.IsOrphaned = orphanStatus;
                }
            });
        }

        public void RemoveConnectedObject(IPBSObject pbsObject)
        {
            var connection = GetConnections().FirstOrDefault(c => c.TargetPbsObject.Equals(pbsObject));
            DeleteConnectionFromDb(connection);
        }

        private void DeleteConnectionFromDb(PBSConnection connection)
        {
            if (connection == null) return;
            
            _connections = null;

            connection.DeleteFromDb();

            Transaction.Current.OnCommited(() =>
            {
                Logger.Info("connection removed from " + _pbsObject.ED.Name + " " + (connection.IsOutgoing ? "OUTGOING" : "INCOMING"));
                OnConnectionDeleted(connection);
            });
        }




        public event Action<PBSConnection> ConnectionCreated;
        public event Action<PBSConnection> ConnectionDeleted;

        protected virtual void OnConnectionCreated(PBSConnection connection)
        {
            _connections = null;
            ConnectionCreated?.Invoke(connection);
        }

        protected virtual void OnConnectionDeleted(PBSConnection connection)
        {
            _connections = null;
            ConnectionDeleted?.Invoke(connection);
        }

        public void AttachConnection(PBSConnection connection)
        {
            connection.InsertToDb();

            Transaction.Current.OnCommited(() =>
            {
                Logger.Info("connection added to " + _pbsObject.ED.Name + " " + (connection.IsOutgoing ? "OUTGOING" : "INCOMING"));
                OnConnectionCreated(connection);
            });
        }

     

        public void SetWeight(IPBSObject pbsObject, double weight)
        {
            pbsObject.ThrowIfNotType<IPBSAcceptsCore>(ErrorCodes.OnlyConsumersHaveWeight);

            var connection = GetConnectionByObject(pbsObject);

            if (connection == null) return;

            connection.IsOutgoing.ThrowIfFalse(ErrorCodes.ConnectionMustBeOutgoing);

            connection.Weight = weight;

            connection.UpdateWeightToSql();
        }



        public IDictionary<string,object> ToDictionary()
        {
            var result = GetConnections().Where(c => c.IsOutgoing).ToDictionary("c", c => c.ToDictionary());

            return result;
        }

        public void SendEventToNetwork(PBSEventArgs pbsEventArgs)
        {
            foreach (var node in NetworkNodes)
            {
                var handler = node as IPBSEventHandler;
                handler?.HandlePBSEvent(_pbsObject,pbsEventArgs);
            }
        }

        public void DebugInfo(IDictionary<string, object> info)
        {
            var connections = GetConnections();

            var counter = 0;

            foreach (var pbsConnection in connections)
            {
                info.Add("c" + counter++, pbsConnection.DebugDictionary());
            }

        }



        public List<IPBSObject> NetworkNodes
        {
            get
            {
                var collected = new Dictionary<long, IPBSObject> { { _pbsObject.Eid, _pbsObject } };

                CollectNodes(collected);

                WriteLog("network size " + collected.Count +  " " + _pbsObject.ED.Name );
                return collected.Values.ToList();
            }
        }

        private int _outConnections = -1;
        private int GetOutConnectionsMax()
        {
            if (_outConnections < 0)
            {
                if (_pbsObject.ED.Config.outConnections != null)
                    return (int) _pbsObject.ED.Config.outConnections;

                    Logger.Error("no outConnections was defined for: " + _pbsObject.Definition + " " + _pbsObject.ED.Name);
                    _outConnections = 0;
            }
            return _outConnections;
            
        }

        private int _inConnections = -1;
        private int GetInConnectionsMax()
        {
            if (_inConnections < 0)
            {
                if (_pbsObject.ED.Config.inConnections != null)
                    return (int)_pbsObject.ED.Config.inConnections;

                Logger.Error("no inConnections was defined for: " + _pbsObject.Definition + " " + _pbsObject.ED.Name);
                _inConnections = 0;
            }
            return _inConnections;
            
        }


        public void CheckNewConnection(IPBSObject target, bool isOutGoing)
        {
            _pbsObject.IsFullyConstructed().ThrowIfFalse(ErrorCodes.ObjectNotFullyConstructed);

            if (isOutGoing)
            {
                GetOutConnectionsMax().ThrowIfLessOrEqual(OutConnections.Count, ErrorCodes.SourceOutOfOutConnections);

                PBSConnectionValidator.Validate(_pbsObject, target).ThrowIfError();

            }
            else
            {
                GetInConnectionsMax().ThrowIfLessOrEqual(InConnections.Count, ErrorCodes.TargetOutOfInConnections);
            }

            ContainsConnection(target).ThrowIfTrue(ErrorCodes.NodeAlreadyConnected);
            
        }


        public void BreakConnection(IPBSObject targetNode, Character issuer)
        {
            _pbsObject.CheckAccessAndThrowIfFailed(issuer);

            RemoveConnectedObject(targetNode);

            targetNode.ConnectionHandler.RemoveConnectedObject(_pbsObject);

            Transaction.Current.OnCommited(() =>
            {
                //do orphan stuff
                var sourceNetwork = _pbsObject.ConnectionHandler.NetworkNodes.ToArray();
                var targetNetwork = targetNode.ConnectionHandler.NetworkNodes.ToArray();

                var orphanStatus = !sourceNetwork.Any(n => n is PBSDockingBase);

                foreach (var pbsObject in sourceNetwork)
                {
                    pbsObject.IsOrphaned = orphanStatus;
                }

                orphanStatus = !targetNetwork.Any(n => n is PBSDockingBase);

                foreach (var pbsObject in targetNetwork)
                {
                    pbsObject.IsOrphaned = orphanStatus;
                }
            });
        }

        [CanBeNull]
        private PBSConnection GetConnectionByObject( IPBSObject pbsObject)
        {
            return GetConnections().FirstOrDefault(c => c.TargetPbsObject.Equals(pbsObject));
        }

        private bool ContainsConnection( IPBSObject pbsObject)
        {
            return GetConnections().Any(c => c.TargetPbsObject.Equals(pbsObject));
        }

        public void CollectNodes( IDictionary<long,IPBSObject> collected)
        {
            var connections = GetConnections();
           
            foreach (var connection in connections)
            {
                var connectedUnit = connection.GetAsUnit();
                if (connectedUnit == null) continue;

                if (collected.ContainsKey(connectedUnit.Eid))
                {
                    continue;
                }
                
                collected.Add( connectedUnit.Eid,connection.TargetPbsObject);

                connection.TargetPbsObject.ConnectionHandler.CollectNodes(collected);
            }
        }

        public List<PBSConnection> InConnections
        {
            get { return GetConnections().Where(c => !c.IsOutgoing).ToList(); }
        }

        public List<PBSConnection> OutConnections
        {
            get { return GetConnections().Where(c => c.IsOutgoing).ToList(); }
        }
    }


}
