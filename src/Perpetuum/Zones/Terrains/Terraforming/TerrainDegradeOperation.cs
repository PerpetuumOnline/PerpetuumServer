using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Terrains.Terraforming.Operations;

namespace Perpetuum.Zones.Terrains.Terraforming
{
    public class TerrainDegradeOperation : ITerraformingOperation
    {
        private readonly Area _workArea;

        public TerrainDegradeOperation(Area workArea)
        {
            _workArea = workArea;
        }

        public void Prepare(IZone zone) {}

        public void DoTerraform(IZone zone)
        {
            var x = FastRandom.NextInt(_workArea.Width - 1);
            var y = FastRandom.NextInt(_workArea.Height - 1);
            var p = new Position(x, y);

            PBSHelper.DegradeTowardsOriginal(zone, p);  
        }

        public Area TerraformArea { get; private set; }
        public void AcceptVisitor(TerraformingOperationVisitor visitor)
        {
            visitor.VisitTerraformingOperation(this);
        }
    }
}
