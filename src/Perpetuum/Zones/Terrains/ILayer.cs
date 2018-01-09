namespace Perpetuum.Zones.Terrains
{
    public interface ILayer
    {
        LayerType LayerType { get; }

        int Width { get; }
        int Height { get; }
    }

    public interface ILayer<T> : ILayer, INotifyLayerUpdated, IUpdateableLayer
    {
        T[] RawData { get; }
        
        T GetValue(int x, int y);
        void SetValue(int x, int y,T value);

        T this[int x, int y] { get; set; }

        T[] GetArea(Area area);
        void SetArea(Area area, T[] data);
    }
}