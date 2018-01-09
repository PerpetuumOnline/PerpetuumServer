using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Modules.Terraforming
{

    /// <summary>
    /// Using this module players can grow a wall plant
    /// </summary>
    public class WallBuilderModule : ActiveModule
    {
        public WallBuilderModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
        }

        protected override void OnAction()
        {
            var terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);

            var player = (ParentRobot as Player).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            (!player.InZone || player.States.Dead).ThrowIfTrue(ErrorCodes.NoError);

            ForceGrowWall(Zone, terrainLock.Location);
            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);
            ConsumeAmmo();
        }

        /// <summary>
        /// Forces a wall plant to grow. WallBuilderModule
        /// </summary>
        private static void ForceGrowWall(IZone zone, Position position)
        {
            if (zone == null) 
                return;

            var x = position.intX;
            var y = position.intY;

            var plantInfo = zone.Terrain.Plants[x, y];
            plantInfo.type.ThrowIfEqual(PlantType.NotDefined,ErrorCodes.NoPlantOnTile);

            var plantRule = zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);
            if ( plantRule == null )
                return;

            plantRule.AllowedOnNonNatural.ThrowIfFalse(ErrorCodes.InvalidPlant);

            using (new TerrainUpdateMonitor(zone))
            {
                if (plantInfo.IsPlantOnMaximumState(plantRule))
                {
                    //yes maximum state
                    plantInfo.IsHealthOnMaximum(plantRule).ThrowIfTrue(ErrorCodes.MaximumPlantStateReached);

                    // heal it
                    plantInfo.HealPlant(plantRule);
                    zone.Terrain.Plants[x,y] = plantInfo;
                }
                else
                {
                    var healthRatio = plantInfo.GetHealthRatio(plantRule);
                    plantInfo.state++;

                    var newMaxHealth = plantRule.Health[plantInfo.state];
                    plantInfo.health = (byte)(newMaxHealth * healthRatio).Clamp(0, 255);

                    //heal anyway
                    plantInfo.HealPlant(plantRule);

                    var blockInfo = zone.Terrain.Blocks[x, y];

                    if (plantRule.IsBlocking(plantInfo.state))
                    {
                        blockInfo.Plant = true;
                        blockInfo.Height = plantRule.GetBlockingHeight(plantInfo.state);
                    }
                    else
                    {
                        blockInfo.Plant = false;
                        blockInfo.Height = 0;
                    }

                    zone.Terrain.Blocks[x,y] = blockInfo;
                    zone.Terrain.Plants[x,y] = plantInfo;
                }
            }
        }
    }
}
