using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Zones.PBS.ArmorRepairers
{
    /// <summary>
    /// Repairs the target nodes
    /// </summary>
    public class PBSArmorRepairerNode : PBSActiveObject,  IPBSAcceptsCore, IPBSUsesCore
    {
        private readonly CoreUseHandler<PBSArmorRepairerNode> _coreUseHandler; 

        public PBSArmorRepairerNode()
        {
            _coreUseHandler = new CoreUseHandler<PBSArmorRepairerNode>(this, new EnergyStateFactory(this));
        }

        public ICoreUseHandler CoreUseHandler { get { return _coreUseHandler; } }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            _coreUseHandler.OnUpdate(time);

        }


        public bool TryCollectCoreConsumption(out double coreDemand)
        {
            var connectedObjectNotWithFullArmor =
                 ConnectionHandler.OutConnections
                    .Select(c => c.TargetPbsObject)
                    .Cast<Unit>()
                    .Count(u => u.Armor.Ratio(u.ArmorMax) < 1.0);

            coreDemand = this.GetCoreConsumption()*connectedObjectNotWithFullArmor;

            return true;

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


        private int _chargeAmount;
        public int GetChargeAmount()
        {
            return PBSHelper.LazyInitChargeAmount(this, ref _chargeAmount);
        }


        private class EnergyStateFactory : IEnergyStateFactory<PBSArmorRepairerNode>
        {
            private readonly PBSArmorRepairerNode _node;

            public EnergyStateFactory(PBSArmorRepairerNode node)
            {
                _node = node;
            }

            public WarmUpRawCoreState<PBSArmorRepairerNode> CreateWarmUpEnergyState()
            {
                return new WarmUpEnergyState(_node);
            }

            public ActiveRawCoreState<PBSArmorRepairerNode> CreateActiveEnergyState()
            {
                return new ActiveEnergyState(_node);
            }
        }

        private class WarmUpEnergyState : WarmUpCoreUserNodeState<PBSArmorRepairerNode>
        {
            public WarmUpEnergyState(PBSArmorRepairerNode owner) : base(owner)
            {
            }
        }
       
        private class ActiveEnergyState  : ActiveCoreUserNodeState<PBSArmorRepairerNode>
        {
            public ActiveEnergyState(PBSArmorRepairerNode owner) : base(owner)
            {
            }

            protected override void PostCoreSubtract()
            {

                //itt az van hogy ugyan a corebol consumptiont von le a mukodeshez, de charge amount of armort javit
                var armorToSpread = (double)Owner.GetChargeAmount();

                var unitsToRepair = new List<KeyValuePair<Unit, double>>();
                foreach (var connection in Owner.ConnectionHandler.OutConnections)
                {

                    var targetUnit = connection.TargetPbsObject as Unit;

                    if (targetUnit != null)
                    {
                        if (targetUnit.ArmorMax <= 0) continue;

                        //armor full
                        if (targetUnit.Armor >= targetUnit.ArmorMax) continue;

                        unitsToRepair.Add(new KeyValuePair<Unit, double>(targetUnit, targetUnit.Armor / targetUnit.ArmorMax));
                    }
                }


                if (unitsToRepair.Count == 0)
                {
                    //semmi dolgunk, nincs kit repairolni
                    return;
                }


                foreach (var unit in unitsToRepair.OrderBy(p => p.Value).Select(p => p.Key))
                {
                    if (armorToSpread <= 0) return; //no more armor to spread

                    var armorMissing = unit.ArmorMax - unit.Armor;

                    armorMissing = Math.Min(armorMissing, armorToSpread);

                    unit.Armor += armorMissing;

                    armorToSpread -= armorMissing;

                    var unit1 = unit;
                    Owner.Zone.CreateBeam(BeamType.pbs_repair, b => b.WithSource(Owner)
                        .WithTarget(unit1)
                        .WithState(BeamState.Hit)
                        .WithVisibility(200)
                        .WithBulletTime(60)
                        .WithDuration(1337));
                }
            }
        }

      
    }

}
