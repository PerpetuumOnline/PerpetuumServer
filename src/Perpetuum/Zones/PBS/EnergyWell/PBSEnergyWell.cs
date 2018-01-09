using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Zones.PBS.Reactors;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Zones.PBS.EnergyWell
{
    //ez aktiv object, nem pedig core use object

    /// <summary>
    /// Drains energy mineral and transfers energy to reactors
    /// </summary>
    public class PBSEnergyWell : PBSActiveObject
    {
        private readonly MaterialHelper _materialHelper;
        private double _lastCoreUsed;
        private bool _wasMineralCollected;

        public bool IsDepleted { get; private set; }

        public PBSEnergyWell(MaterialHelper materialHelper)
        {
            _materialHelper = materialHelper;
        }

        private int WorkRange
        {
            get
            {
                if (ED.Config.item_work_range != null)
                    return (int) ED.Config.item_work_range;

                Logger.Error("no emitradius defined for " + ED.Definition + " " + ED.Name);
                return 10;
            }
        }

        private double TransferEfficiency
        {
            get
            {
                if (ED.Config.transferEfficiency != null)
                    return (double) ED.Config.transferEfficiency;

                Logger.Error("no transfer efficiency is defined for " + ED.Definition + " " + ED.Name);
                return 0.98;
            }
        }

        private double CoreTransferred
        {
            get
            {
                if (ED.Config.coreTransferred != null)
                    return (double)ED.Config.coreTransferred;

                Logger.Error("no CoreTransferred is defined for " + ED.Definition + " " + ED.Name);
                return 0.98;
            }
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            info[k.lastUsedCore] = _lastCoreUsed; //ennyit pumpalt szet a halozaton utoljara
            info["wasMineralCollected"] = _wasMineralCollected ? 1 : 0;

            if (IsDepleted)
            {
                info[k.depleted] = true;
            }

            return info;
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();

            info[k.lastUsedCore] = _lastCoreUsed;
            info["wasMineralCollected"] = _wasMineralCollected ? 1 : 0;

            if (IsDepleted)
            {
                info[k.depleted] = true;
            }

            return info;
        }

        public override void OnLoadFromDb()
        {
            if (DynamicProperties.Contains(k.depleted))
            {
                IsDepleted = DynamicProperties.GetOrAdd<int>(k.depleted) == 1;
            }
            else
            {
                DynamicProperties.Update(k.depleted, IsDepleted ? 1 : 0);   
            }

            base.OnLoadFromDb();
        }

        public override void OnInsertToDb()
        {
            SaveToDb();
            base.OnInsertToDb();
        }

        public override void OnUpdateToDb()
        {
            SaveToDb();
            base.OnUpdateToDb();
        }

        private void SaveToDb()
        {
            DynamicProperties.Update(k.depleted, IsDepleted ? 1 : 0);
        }

        public List<ItemInfo> ExtractWithinRange(MineralLayer layer, Point location, int range, uint amount)
        {
            var nodes = layer.GetNodesWithinRange(location, range).OrderBy(n => n.Area.SqrDistance(location));

            var result = new List<ItemInfo>();

            foreach (var node in nodes)
            {
                var needed = amount - result.Sum(r => r.Quantity);
                if (needed <= 0)
                    break;

                var nearestNode = node.GetNearestMineralPosition(location);
                if (nearestNode.Distance(location) > range)
                    continue;

                var e = new MineralExtractor(nearestNode, (uint)needed,_materialHelper);
                layer.AcceptVisitor(e);
                result.AddRange(e.Items);
            }

            return result;
        }

        protected override void PBSActiveObjectAction(IZone zone)
        {
            if (IsDepleted) 
                return; //kifogyott alola a mineral

            _lastCoreUsed = 0;

            // van-e reaktor a networkbe akinek hianyzik egy toltes
            var reactors = ConnectionHandler.NetworkNodes.OfType<PBSReactor>().Where(r => (r.CoreMax - r.Core) > CoreTransferred && r.OnlineStatus).ToList();
            if (reactors.Count == 0)
                return; //no reactor to feed

            var mineralLayer = zone.Terrain.GetMaterialLayer(MaterialType.EnergyMineral) as MineralLayer;
            if (mineralLayer == null)
            {
                Logger.Error("mineral not found on zone " + MaterialType.EnergyMineral + " zoneID:" + zone.Configuration.Id);
                return;
            }

            //egy ciklusra mennyit banyaszik ebbol a fajtabol
            //ennyit akar maximum kiszedni a sok csempebol
            var extractedMaterials = ExtractWithinRange(mineralLayer, CurrentPosition, WorkRange, (uint)CoreTransferred);

            if (extractedMaterials.Count <= 0)
            {
                //nemtom, barmi, le is kapcsolhatja magat
                _wasMineralCollected = false;

                IsDepleted = true;
                //mineral got depleted
                Logger.Info("energy well got depleted " + this);
                SetOnlineStatus(false, false);
                PBSHelper.WritePBSLog(PBSLogType.wellDepleted, Eid, Definition, Owner, zoneId: zone.Configuration.Id);
                return;
            }
            
            //juhe volt mineral, pakoljunk coret a network reactorjaiba
            var sumCollected = extractedMaterials.Sum(i => (double) i.Quantity);
            Logger.Info(sumCollected + " mineral was drilled by " + ED.Name);

            sumCollected *= TransferEfficiency;
            Logger.Info(sumCollected + " core will be distributed ");

            var amountPerReactor = sumCollected/reactors.Count;
            Logger.Info(reactors.Count + " reactors. per node amount: " + amountPerReactor);

            foreach (var pbsReactor in reactors)
            {
                pbsReactor.CoreFromEnergyWell((int) amountPerReactor);
            }

            //output
            _lastCoreUsed = sumCollected;
            _wasMineralCollected = true;
        }
    }
}
