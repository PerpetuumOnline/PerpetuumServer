using System.Collections.Generic;
using Perpetuum.Log;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS.ProductionNodes
{
    public class PBSProductionFacilityNodeHelper 
    {
        private readonly PBSProductionFacilityNode _pbsUnit;

        public PBSProductionFacilityNodeHelper(PBSProductionFacilityNode pbsProductionFacilityNode)
        {
            _pbsUnit = pbsProductionFacilityNode;

            if (_pbsUnit.ConnectionHandler is INotifyConnectionModified n)
            {
                n.ConnectionCreated += OnConnectionCreated;
            }
        }

        private void OnConnectionCreated(PBSConnection connection)
        {
            //itt kell lekezelni, hogy be lett kotve

            if (!connection.IsOutgoing) return;

            var dockingBase = connection.TargetPbsObject as PBSDockingBase;

            if (dockingBase == null)
            {
                Logger.Error("WTF connected object is not a docking base!!! ");
                return;
            }

            _pbsUnit.SendMessageToProductionEngineOnConnection(dockingBase);
        }

        public void Init()
        {
            if (_pbsUnit.DynamicProperties.Contains(k.productionLevelCurrent))
            {
                _pbsUnit.PreLevel = _pbsUnit.DynamicProperties.GetOrAdd<double>(k.productionLevelCurrent);
            }
        }

        public void AddToDictionary(IDictionary<string, object> info)
        {
            info.Add(k.productionLevelCurrent, _pbsUnit.PreLevel);
        }

        public void OnSave()
        {
            _pbsUnit.DynamicProperties.Update(k.productionLevelCurrent,_pbsUnit.PreLevel);
        }
    }
}