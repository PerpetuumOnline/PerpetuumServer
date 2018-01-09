using System.Drawing;

namespace Perpetuum.Zones.Terrains.Terraforming.Operations
{
   
    public class BlurTerraformingOperation : TerraformingOperation
    {
        public BlurTerraformingOperation(Position centerPosition, int radius, int plantDamage) : base(centerPosition, plantDamage)
        {
            TerraformArea = Area.FromRadius(centerPosition, radius);
        }

        public override void AcceptVisitor(TerraformingOperationVisitor visitor)
        {
            visitor.VisitBlurTerraformingOperation(this);
        }

        protected override int ProduceDirection(IZone zone, int x, int y)
        {
            var p = new Point(x, y);

            var sum = 0.0;
            foreach (var n in p.GetNeighbours())
            {
                if (zone.IsValidPosition(n.X, n.Y))
                {
                    sum += zone.Terrain.Altitude[n.X, n.Y];
                }
            }

            var blurred = sum / 8.0;
            var newAltitude = (ushort)(blurred.Clamp(ushort.MinValue, ushort.MaxValue));
            return newAltitude - zone.Terrain.Altitude[x, y];
        }
    }

}