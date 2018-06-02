using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Modules
{
    public class DrillerModule : GathererModule
    {
        private readonly RareMaterialHandler _rareMaterialHandler;
        private readonly MaterialHelper _materialHelper;
        private readonly ItemProperty _miningAmountModifier;

        public DrillerModule(CategoryFlags ammoCategoryFlags,RareMaterialHandler rareMaterialHandler,MaterialHelper materialHelper) : base(ammoCategoryFlags, true)
        {
            _rareMaterialHandler = rareMaterialHandler;
            _materialHelper = materialHelper;
            _miningAmountModifier = new MiningAmountModifierProperty(this);
            AddProperty(_miningAmountModifier);

        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        private class MiningAmountModifierProperty : ModuleProperty
        {
            private readonly DrillerModule _module;

            public MiningAmountModifierProperty(DrillerModule module) : base(module, AggregateField.mining_amount_modifier)
            {
                _module = module;
                AddEffectModifier(AggregateField.effect_mining_amount_modifier);
            }

            protected override double CalculateValue()
            {
                if (module.ParentRobot == null)
                    return 1.0;

                var m = module.ParentRobot.GetPropertyModifier(AggregateField.mining_amount_modifier);

                var ammo = (MiningAmmo)_module.GetAmmo();
                ammo?.ApplyMiningAmountModifier(ref m);
                module.ParentRobot?.ApplyEffectPropertyModifiers(AggregateField.effect_mining_amount_modifier,ref m);

                return m.Value;
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.mining_amount_modifier:
                case AggregateField.effect_mining_amount_modifier:
                    {
                        _miningAmountModifier.Update();
                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        protected override void OnAction()
        {
            var zone = Zone;
            if ( zone != null )
                DoExtractMinerals(zone);

            ConsumeAmmo();
        }

        public List<ItemInfo> Extract(MineralLayer layer, Point location, uint amount)
        {
            if (!layer.HasMineral(location))
                return new List<ItemInfo>();

            var extractor = new MineralExtractor(location, amount,_materialHelper);
            layer.AcceptVisitor(extractor);
            return new List<ItemInfo>(extractor.Items);
        }

        private void DoExtractMinerals(IZone zone)
        {
            var terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);

            var ammo = GetAmmo() as MiningAmmo;
            if (ammo == null)
                return;

            var materialInfo = _materialHelper.GetMaterialInfo(ammo.MaterialType);

            CheckEnablerEffect(materialInfo);

            var mineralLayer = zone.Terrain.GetMineralLayerOrThrow(materialInfo.Type);
            var materialAmount = materialInfo.Amount * _miningAmountModifier.Value;

            var extractedMaterials = Extract(mineralLayer, terrainLock.Location,(uint) materialAmount);
            extractedMaterials.Count.ThrowIfEqual(0, ErrorCodes.NoMineralOnTile);

            extractedMaterials.AddRange(_rareMaterialHandler.GenerateRareMaterials(materialInfo.EntityDefault.Definition));

            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);

            using (var scope = Db.CreateTransaction())
            {
                Debug.Assert(ParentRobot != null, "ParentRobot != null");
                var container = ParentRobot.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();

                var player = ParentRobot as Player;
                Debug.Assert(player != null,"player != null");

                foreach (var material in extractedMaterials)
                {
                    var item = (Item)Factory.CreateWithRandomEID(material.Definition);

                    item.Owner = Owner;
                    item.Quantity = material.Quantity;

                    container.AddItem(item, true);

                    var drilledMineralDefinition = material.Definition;
                    var drilledQuantity = material.Quantity;

                    player.MissionHandler.EnqueueMissionEventInfo(new DrillMineralEventInfo(player,drilledMineralDefinition,drilledQuantity,terrainLock.Location));
                    player.Zone?.MiningLogHandler.EnqueueMiningLog(drilledMineralDefinition,drilledQuantity);
                }

                //save container
                container.Save();
                OnGathererMaterial(zone, player);

                Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                scope.Complete();
            }
        }

        private void CheckEnablerEffect(MaterialInfo materialInfo)
        {
            if ( !Zone.Configuration.Terraformable )
                return;

            if (!materialInfo.EnablerExtensionRequired) 
                return;

            var containsEnablerEffect = ParentRobot.EffectHandler.ContainsEffect(EffectCategory.effcat_pbs_mining_tower_effect);
            containsEnablerEffect.ThrowIfFalse(ErrorCodes.MiningEnablerEffectRequired);
        }
    }
}