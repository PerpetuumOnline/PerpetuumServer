using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class OreLayer : MineralLayer
    {
        public OreLayer(int width, int height, IMineralConfiguration configuration, IMineralNodeRepository nodeRepository, IMineralNodeGeneratorFactory nodeGeneratorFactory)
            : base(width,height,configuration, nodeRepository,nodeGeneratorFactory)
        {
        }

        public override void AcceptVisitor(MineralLayerVisitor visitor)
        {
            visitor.VisitOreLayer(this);
        }
    }
}