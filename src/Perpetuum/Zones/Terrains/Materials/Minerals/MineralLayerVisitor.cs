namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class LayerVisitor
    {
        public virtual void VisitLayer(Layer layer)
        {
            
        }
    }

    public class MineralLayerVisitor : LayerVisitor
    {
        public virtual void VisitMineralLayer(MineralLayer layer)
        {
            VisitLayer(layer);            
        }

        public virtual void VisitLiquidLayer(LiquidLayer layer)
        {
            VisitMineralLayer(layer);
        }

        public virtual void VisitOreLayer(OreLayer layer)
        {
            VisitMineralLayer(layer);
        }

        public virtual void VisitGravelLayer(GravelLayer layer)
        {
            VisitOreLayer(layer);
        }
    }
}