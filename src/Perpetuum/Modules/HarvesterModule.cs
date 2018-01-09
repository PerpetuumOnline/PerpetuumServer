using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Materials.Plants.Harvesters;

namespace Perpetuum.Modules
{
    public abstract class GathererModule : ActiveModule
    {
        protected GathererModule(CategoryFlags ammoCategoryFlags, bool ranged = false) : base(ammoCategoryFlags, ranged)
        {
            coreUsage.AddEffectModifier(AggregateField.effect_core_usage_gathering_modifier);
            cycleTime.AddEffectModifier(AggregateField.effect_gathering_cycle_time_modifier);
        }

        private const int MAX_EP_PER_DAY = 720;

        private int CalculateEp()
        {
            var activeGathererModules = ParentRobot.ActiveModules.OfType<GathererModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray();
            if (activeGathererModules.Length == 0)
                return 0;

            var avgCycleTime = activeGathererModules.Select(m => m.CycleTime).Average();

            var t = TimeSpan.FromDays(1).Divide(avgCycleTime);
            var chance = (double) MAX_EP_PER_DAY/t.Ticks;

            chance /= activeGathererModules.Length;

            var rand = FastRandom.NextDouble();
            if (rand <= chance)
                return 1;

            return 0;
        }

        protected void OnGathererMaterial(IZone zone, Player player)
        {
            if (zone.Configuration.Type == ZoneType.Training) return;

            var ep = CalculateEp();

            if (zone.Configuration.IsBeta)
                ep *= 2;

            player.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Gathering, ep);
        }
    }

    public class HarvesterModule : GathererModule
    {
        private readonly PlantHarvester.Factory _plantHarvesterFactory;
        private readonly HarvestingAmountModifierProperty _harverstingAmountModifier;

        public HarvesterModule(CategoryFlags ammoCategoryFlags,PlantHarvester.Factory plantHarvesterFactory) : base(ammoCategoryFlags, true)
        {
            _plantHarvesterFactory = plantHarvesterFactory;
            _harverstingAmountModifier = new HarvestingAmountModifierProperty(this);
            AddProperty(_harverstingAmountModifier);

            cycleTime.AddEffectModifier(AggregateField.effect_harvesting_cycle_time_modifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void UpdateProperty(AggregateField field)
        {
            base.UpdateProperty(field);

            switch (field)
            {
                case AggregateField.harvesting_amount_modifier:
                case AggregateField.effect_harvesting_amount_modifier:
                    {
                        _harverstingAmountModifier.Update();
                        break;
                    }
            }
        }

        protected override void OnAction()
        {
            var zone = Zone;
            if (zone == null)
                return;

            DoHarvesting(zone);
            ConsumeAmmo();
        }

        private void DoHarvesting(IZone zone)
        {
            var terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);
            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);

            using (var scope = Db.CreateTransaction())
            {
                using (new TerrainUpdateMonitor(zone))
                {
                    var plantInfo = zone.Terrain.Plants.GetValue(terrainLock.Location);
                    var amountModifier = _harverstingAmountModifier.GetValueByPlantType(plantInfo.type);
                    IPlantHarvester plantHarvester = _plantHarvesterFactory(zone, amountModifier);
                    var harvestedPlants = plantHarvester.HarvestPlant(terrainLock.Location);

                    Debug.Assert(ParentRobot != null, "ParentRobot != null");
                    var container = ParentRobot.GetContainer();
                    Debug.Assert(container != null, "container != null");
                    container.EnlistTransaction();

                    var player = ParentRobot as Player;
                    Debug.Assert(player != null,"player != null");

                    foreach (var extractedMaterial in harvestedPlants)
                    {
                        var item = (Item)Factory.CreateWithRandomEID(extractedMaterial.Definition);
                        item.Owner = Owner;
                        item.Quantity = extractedMaterial.Quantity;

                        container.AddItem(item, true);

                        player.MissionHandler.EnqueueMissionEventInfo(new HarvestPlantEventInfo(player, extractedMaterial.Definition, extractedMaterial.Quantity, terrainLock.Location));
                    }

                    //everything went ok, save container
                    container.Save();

                    OnGathererMaterial(zone, player);

                    Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                    scope.Complete();
                }
            }
        }

        private class HarvestingAmountModifierProperty : ModuleProperty
        {
            public HarvestingAmountModifierProperty(HarvesterModule module) : base(module, AggregateField.harvesting_amount_modifier)
            {
                AddEffectModifier(AggregateField.effect_harvesting_amount_modifier);
            }

            protected override double CalculateValue()
            {
                if (module.ParentRobot == null)
                    return 1.0;

                var p = module.ParentRobot.GetPropertyModifier(AggregateField.harvesting_amount_modifier);
                ApplyEffectModifiers(ref p);
                return p.Value;
            }

            public double GetValueByPlantType(PlantType plantType)
            {
                var modifier = AggregateField.undefined;

                switch (plantType)
                {
                    case PlantType.RustBush:
                        {
                            modifier = AggregateField.harvesting_amount_helioptris_modifier;
                            break;
                        }
                    case PlantType.SlimeRoot:
                        {
                            modifier = AggregateField.harvesting_amount_triandlus_modifier;
                            break;
                        }
                    case PlantType.ElectroPlant:
                        {
                            modifier = AggregateField.harvesting_amount_electroplant_modifier;
                            break;
                        }
                    case PlantType.TreeIron:
                        {
                            modifier = AggregateField.harvesting_amount_prismocitae_modifier;
                            break;
                        }
                }

                var property = this.ToPropertyModifier();

                if (module.ParentRobot != null && modifier != AggregateField.undefined)
                {
                    var mod = module.ParentRobot.GetPropertyModifier(modifier);
                    mod.Modify(ref property);
                }

                return property.Value;
            }
        }

    }
}