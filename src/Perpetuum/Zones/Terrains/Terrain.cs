using System.Collections.Generic;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones.Terrains
{
    public class Terrain : ITerrain
    {
        private AltitudeLayer _altitude;
        private SlopeLayer _slope;
        private ILayer<BlockingInfo> _blocks;
        private ILayer<TerrainControlInfo> _controls;
        private ILayer<PlantInfo> _plants;
        private ILayer<bool> _passable;

        private readonly Dictionary<LayerType, ILayer> _layers = new Dictionary<LayerType, ILayer>();

        private void AddLayer(ILayer layer)
        {
            _layers[layer.LayerType] = layer;
        }

        public AltitudeLayer Altitude
        {
            get { return _altitude; }
            set
            {
                _altitude = value;
                AddLayer(_altitude);
            }
        }

        public SlopeLayer Slope
        {
            get { return _slope; }
            set { _slope = value; }
        }

        public ILayer<BlockingInfo> Blocks
        {
            get { return _blocks; }
            set
            {
                _blocks = value;
                AddLayer(_blocks);
            }
        }

        public ILayer<TerrainControlInfo> Controls
        {
            get { return _controls; }
            set
            {
                _controls = value;
                AddLayer(_controls);
            }
        }

        public ILayer<PlantInfo> Plants
        {
            get { return _plants; }
            set
            {
                _plants = value;
                AddLayer(_plants);
            }
        }

        public ILayer<bool> Passable
        {
            get { return _passable; }
            set { _passable = value; }
        }

        [CanBeNull]
        public ILayer GetLayerByType(LayerType type)
        {
            return _layers.GetOrDefault(type);
        }

        public Dictionary<MaterialType,IMaterialLayer> Materials { get; set; } = new Dictionary<MaterialType, IMaterialLayer>();

        public IMaterialLayer GetMaterialLayer(MaterialType type)
        {
            return !Materials.TryGetValue(type, out IMaterialLayer layer) ? null : layer;
        }

        IEnumerable<IMaterialLayer> ITerrain.Materials => Materials.Values;
    }
}