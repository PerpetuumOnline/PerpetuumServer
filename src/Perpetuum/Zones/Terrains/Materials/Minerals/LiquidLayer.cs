using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class LiquidLayer : MineralLayer
    {
        public LiquidLayer(int width, int height, IMineralConfiguration configuration, MineralNodeRepository nodeRepository, IMineralNodeGeneratorFactory nodeGeneratorFactory)
            : base(width,height,configuration, nodeRepository,nodeGeneratorFactory)
        {
        }

        public override void AcceptVisitor(MineralLayerVisitor visitor)
        {
            visitor.VisitLiquidLayer(this);
        }
    }
}