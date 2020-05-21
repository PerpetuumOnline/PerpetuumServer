using Perpetuum.Services.EventServices;
using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class LiquidLayer : MineralLayer
    {
        public LiquidLayer(int width, int height, IMineralConfiguration configuration, IMineralNodeRepository nodeRepository, IMineralNodeGeneratorFactory nodeGeneratorFactory, EventListenerService eventListenerService)
            : base(width, height, configuration, nodeRepository, nodeGeneratorFactory, eventListenerService)
        {
        }

        public override void AcceptVisitor(MineralLayerVisitor visitor)
        {
            visitor.VisitLiquidLayer(this);
        }
    }
}