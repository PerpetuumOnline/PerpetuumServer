namespace Perpetuum.Zones.Terrains.Terraforming.Operations
{
    

    public class LevelTerraformingOperation : TerraformingOperation
    {

        private readonly int _radius;
        

        public LevelTerraformingOperation(Position centerPosition, int radius, int plantDamage) : base(centerPosition, plantDamage)
        {
            _radius = radius;
            TerraformArea = Area.FromRadius(centerPosition, _radius);
            
        }

        public override void AcceptVisitor(TerraformingOperationVisitor visitor)
        {
            visitor.VisitLevelTerraformingOperation(this);
        }

        protected override int ProduceDirection(IZone zone, int x, int y)
        {
            var altitude = zone.Terrain.Altitude;
            var centerAltitude = altitude.GetValue(CenterPosition);

            if (CenterPosition.intX == x && CenterPosition.intY == y)
                return 0;

            var currentAltitude = altitude.GetValue(x, y);
            var altitudeDifference = (centerAltitude - currentAltitude).Clamp(-1*(int) DistanceConstants.MAX_TERRAFORM_LEVEL_DIFFERENCE, (int) DistanceConstants.MAX_TERRAFORM_LEVEL_DIFFERENCE);
            var multiplier = MathHelper.DistanceFalloff(3, _radius, CenterPosition.X, CenterPosition.Y, x+0.5, y+0.5);
            var changeAmount = (int) (altitudeDifference*multiplier);

            return changeAmount;
        }
    }

}