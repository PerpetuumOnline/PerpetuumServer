using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Terraforming;
using Perpetuum.Zones.Terrains.Terraforming.Operations;

namespace Perpetuum.Modules.Terraforming
{
    
    /// <summary>
    /// Multi purpose terraforming module
    /// 
    /// The ammo spawns the actual terraforming operation
    /// </summary>
    public class TerraformMultiModule : ActiveModule
    {
        public TerraformMultiModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
        }

        protected override void OnAction()
        {
            var terrainLock = GetLock() as TerrainLock;
            if (terrainLock == null) 
                return;
            
            var player = (ParentRobot as Player).ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            (!player.InZone || player.States.Dead).ThrowIfTrue(ErrorCodes.PlayerNotFound);

            //only on terraformable zones
            Zone.Configuration.Terraformable.ThrowIfFalse(ErrorCodes.ZoneNotTerraformable);

            //player's altitude VS locked position's altitude -> nem jo a target, mert nem frissul
            var altitude = Zone.Terrain.Altitude.GetAltitudeAsDouble(terrainLock.Location);
            Math.Abs(player.CurrentPosition.intZ - altitude).ThrowIfGreater(DistanceConstants.MAX_TERRAFORM_ALTITUDE_PLAYER_VS_TARGET_DIFFERENCE,ErrorCodes.TargetAltitudeDifferenceExceeded);

            //locked tile is terraformprotected
            var controlInfo = Zone.Terrain.Controls.GetValue(terrainLock.Location);
            controlInfo.IsAnyTerraformProtected.ThrowIfTrue(ErrorCodes.TileTerraformProtected);

            //spawn operation, submit it
            var terraformingOperation = GetTerraformingOperation(terrainLock);

            //prepare function runs inside here => checks if a player is in a safe area
            Zone.TerraformHandler.EnqueueTerraformingOperation(terraformingOperation);

            //gfx
            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);
            ConsumeAmmo();
        }

        private const int PLANT_DAMAGE = 80;
        private const int TERRAIN_CHANGE_AMOUNT = 16;
        private const int TERRAFORM_MAX_RADIUS = 5;
        private TerraformingOperation GetTerraformingOperation(TerrainLock terrainLock )
        {
            (terrainLock.TerraformType == TerraformType.Undefined ||
             terrainLock.TerraformDirection == TerraformDirection.Undefined ||
             terrainLock.Radius == 0).ThrowIfTrue(ErrorCodes.BadTerraformLock);

            var position = terrainLock.Location;
            var radius = terrainLock.Radius.Clamp(0, TERRAFORM_MAX_RADIUS);
            var falloff = terrainLock.Falloff.Clamp(0, radius);

            switch (terrainLock.TerraformType)
            {
                case TerraformType.Blur:
                    return new BlurTerraformingOperation(position, radius, PLANT_DAMAGE);
                    
                case TerraformType.Level:
                    return new LevelTerraformingOperation(position, radius, PLANT_DAMAGE);
                    
                case TerraformType.Simple:

                    (terrainLock.TerraformDirection == TerraformDirection.Undefined).ThrowIfTrue(ErrorCodes.BadTerraformLock);

                    var sign = terrainLock.TerraformDirection == TerraformDirection.Lower ? -1 : 1;

                    var terrainChangeAmount = TERRAIN_CHANGE_AMOUNT*sign;
                    return new SimpleTileTerraformingOperation(position, terrainChangeAmount, PLANT_DAMAGE, radius, falloff);
            }

            throw new PerpetuumException(ErrorCodes.BadTerraformLock);
        }
        
    }
    
   
}
