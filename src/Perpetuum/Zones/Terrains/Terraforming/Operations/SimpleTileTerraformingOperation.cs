namespace Perpetuum.Zones.Terrains.Terraforming.Operations
{
   

    public class SimpleTileTerraformingOperation : TerraformingOperation
    {
        private readonly int _terrainChangeAmount;
        private readonly int _nearRadius;
        private readonly int _farRadius;

        /// <summary>
        /// Prepares an operation for a single tile
        /// 
        ///     #    =>  #.
        ///              ..
        /// 
        /// </summary>
        public SimpleTileTerraformingOperation(Position centerPosition,int terrainChangeAmount, int plantDamage, int radius,int falloff) : base(centerPosition, plantDamage)
        {
            _terrainChangeAmount = terrainChangeAmount;
            var a = Area.FromRadius(centerPosition, (radius - 1).Clamp(0, 5));
            TerraformArea = new Area(a.X1,a.Y1,a.X2 + 1,a.Y2 + 1);

            _nearRadius = (radius - falloff).Clamp(0, int.MaxValue);
            _farRadius = radius;

        }

        public override void AcceptVisitor(TerraformingOperationVisitor visitor)
        {
            visitor.VisitSingleTileTerraformingOperation(this);
        }
        
        protected override int ProduceDirection(IZone zone, int x, int y)
        {
            var multiplier = 1.0;

            var cp = TerraformArea.CenterPrecise;

            if (TerraformArea.Width > 2)
            {
                multiplier = MathHelper.DistanceFalloff(_nearRadius, _farRadius, cp.intX+1, cp.intY+1, x + 0.5, y + 0.5);
            }

            return (int) (_terrainChangeAmount*multiplier);
        }
    }




}