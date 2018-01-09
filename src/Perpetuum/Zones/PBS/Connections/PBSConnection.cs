using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Units;
using Perpetuum.Zones.PBS.ArmorRepairers;
using Perpetuum.Zones.PBS.ControlTower;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.EffectNodes;
using Perpetuum.Zones.PBS.EnergyWell;
using Perpetuum.Zones.PBS.HighwayNode;
using Perpetuum.Zones.PBS.ProductionNodes;
using Perpetuum.Zones.PBS.Reactors;
using Perpetuum.Zones.PBS.Turrets;

namespace Perpetuum.Zones.PBS.Connections
{
    /// <summary>
    /// Represents a connection between two pbs nodes
    /// </summary>
    public class PBSConnection : IEquatable<PBSConnection>
    {
        private double _weight;

        public int Id;
        private readonly IPBSObject _sourcePbsObject;
        public IPBSObject TargetPbsObject { get; private set; }
        public bool IsOutgoing { get; private set; }
        public bool IsIncoming { get { return !IsOutgoing; } }

        public PBSConnection(int id, IPBSObject connected, IPBSObject source, bool isOutGoing, double weight = 1.0)
        {
            TargetPbsObject = connected;
            IsOutgoing = isOutGoing;
            Weight = weight;
            Id = id;
            _sourcePbsObject = source;
        }
         public override string ToString()
         {
             var s = (Unit) _sourcePbsObject;
             var t = (Unit) TargetPbsObject;

             return (IsOutgoing ? "OUT " : "IN ") + s.ED.Name + (IsOutgoing ? " --> " : " <-- ") + t.ED.Name;
         }

        public double Weight
        {
            get { return _weight; }
            set { _weight = value.Clamp(0, 100); }
        }

       
        


        public PBSConnection(IPBSObject targetNode, IPBSObject sourceNode, bool isOutGoing)
        {
            TargetPbsObject = targetNode;
            IsOutgoing = isOutGoing;
            Weight = isOutGoing ? 50 : 0.0;
            _sourcePbsObject = sourceNode;
        }

        public bool Equals(PBSConnection other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            return TargetPbsObject.GetHashCode();
        }

        public Dictionary<string, object> ToDictionary()
        {
            var info = new Dictionary<string, object>
                           {
                               {k.ID, Id},
                               {k.type, (int) GetDisplayType() },

                           };

            AddWeight(info);

            var unit = TargetPbsObject as Unit;
            if (unit != null)
                info.Add(k.eid, unit.Eid);

            return info;

        }

        public Dictionary<string, object> DebugDictionary()
        {
            var info = new Dictionary<string, object>
            {
                {k.type, GetDisplayType().ToString()},
                {"direction", IsOutgoing ? "OUT" : "IN"},
                
            };

            var unit = TargetPbsObject as Unit;
            if (unit != null)
                info.Add("e", unit.ED.Name);

            return info;

        }

       


        private void AddWeight(Dictionary<string,object> info )
        {
            if (_sourcePbsObject is IPBSCorePump && TargetPbsObject is IPBSAcceptsCore)
            {
                info.Add(k.weight, Weight);
            }
        }

        private PBSConnectionType GetDisplayType()
        {
            if (_sourcePbsObject is PBSHighwayNode && TargetPbsObject is PBSHighwayNode)
            {
                var sHn = (PBSHighwayNode) _sourcePbsObject;
                var tHn = (PBSHighwayNode) TargetPbsObject;

                if (sHn.IsGoodHighwayTarget() && tHn.IsGoodHighwayTarget())
                {
                    return PBSConnectionType.highway;
                }

            }

            if (_sourcePbsObject is PBSDockingBase)
            {
                return PBSConnectionType.control;
            }
            
            if (TargetPbsObject is PBSEnergyWell)
            {
                return PBSConnectionType.control;
            }

            if (_sourcePbsObject is PBSReactor && !(TargetPbsObject is IPBSAcceptsCore))
            {
                return PBSConnectionType.control;
            }

            if (_sourcePbsObject is PBSHighwayNode && TargetPbsObject is PBSHighwayNode)
            {
                return PBSConnectionType.energy;
            }
            
            if (_sourcePbsObject is IPBSCorePump && TargetPbsObject is IPBSAcceptsCore)
            {
                return PBSConnectionType.energy;
            }
            
            if (_sourcePbsObject is IPBSCorePump)
            {
                return PBSConnectionType.energy;
            }
            

            if (_sourcePbsObject is PBSProductionFacilityNode || (_sourcePbsObject is PBSFacilityUpgradeNode && TargetPbsObject is PBSProductionFacilityNode  ) )
            {
                return PBSConnectionType.production;
            }

            if (_sourcePbsObject is PBSEffectNode)
            {
                return PBSConnectionType.effect;
            }

            if (_sourcePbsObject is PBSArmorRepairerNode)
            {
                return PBSConnectionType.armorRepair;
            }

            if (_sourcePbsObject is PBSControlTower)
            {
                return PBSConnectionType.control;
            }

            if (TargetPbsObject is PBSControlTower )
            {
                return PBSConnectionType.control;
            }

           
            if (_sourcePbsObject is PBSTurret)
            {
                return PBSConnectionType.control;
            }


            return PBSConnectionType.generic;

        }

        public void UpdateWeightToSql()
        {
            Db.Query().CommandText("update pbsconnections set weight=@weight where id=@id")
                .SetParameter("@id", Id)
                .SetParameter("@weight", Weight)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public Unit GetAsUnit()
        {
            return TargetPbsObject as Unit;
        }

        public void DeleteFromDb()
        {
            WriteLog("deleting connection from sql " + this.ToString());

            if (IsIncoming)
            {
                //it is an INCOMING connection
                TargetPbsObject.ConnectionHandler.RemoveConnectedObject(_sourcePbsObject); 
                return;
            }
            
            Db.Query().CommandText("delete pbsconnections where id=@id")
                .SetParameter("@id", Id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);

        }

       

        public void InsertToDb()
        {
            if (!IsOutgoing) return;

            Id = Db.Query().CommandText("insert pbsconnections (sourceeid,targeteid,weight) values (@source,@target,@weight);SELECT cast(scope_identity() as int)")
                .SetParameter("@source", ((Unit) _sourcePbsObject).Eid)
                .SetParameter("@target", ((Unit) TargetPbsObject).Eid)
                .SetParameter("@weight", Weight)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        private void WriteLog(string message)
        {
            Logger.Info(message);
        }


    }


}
