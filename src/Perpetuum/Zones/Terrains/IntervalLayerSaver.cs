using System;
using System.Threading;
using Perpetuum.Threading.Process;

namespace Perpetuum.Zones.Terrains
{
    public class IntervalLayerSaver<T> : Process where T : struct
    {
        private readonly ILayerFileIO _layerFileIO;
        private readonly ILayer<T> _layer;
        private readonly IZone _zone;
        private int _dirty;

        public delegate IntervalLayerSaver<T> Factory(ILayer<T> layer, IZone zone);

        public IntervalLayerSaver(ILayerFileIO layerFileIO,ILayer<T> layer, IZone zone)
        {
            _layerFileIO = layerFileIO;
            _layer = layer;
            _zone = zone;

            var n = layer as INotifyLayerUpdated;
            if (n == null)
                return;

            n.Updated += (l, x, y) => _dirty = 1;
            n.AreaUpdated += (l, area) => _dirty = 1;
        }

        public override void Stop()
        {
            SaveLayer();
            base.Stop();
        }

        public override void Update(TimeSpan time)
        {
            if ( Interlocked.CompareExchange(ref _dirty,0,1) == 0)
                return;

            SaveLayer();
        }

        private void SaveLayer()
        {
            _layerFileIO.SaveLayerToDisk(_zone,_layer);
        }
    }
}