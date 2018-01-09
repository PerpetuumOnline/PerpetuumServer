namespace Perpetuum.Zones.Terrains
{
    public delegate void LayerUpdated(ILayer layer, int x, int y);
    public delegate void LayerAreaUpdated(ILayer layer, Area area);

    public interface INotifyLayerUpdated
    {
        event LayerUpdated Updated;
        event LayerAreaUpdated AreaUpdated;
    }
}