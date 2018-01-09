using System.Collections.Generic;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones.Terrains
{
    public interface ITerrain
    {
        AltitudeLayer Altitude { get; }
        SlopeLayer Slope { get; }
        ILayer<BlockingInfo> Blocks { get; }
        ILayer<TerrainControlInfo> Controls { get; }
        ILayer<PlantInfo> Plants { get; }
        
        [CanBeNull]
        ILayer<bool> Passable { get; }

        [CanBeNull]
        ILayer GetLayerByType(LayerType type);

        IEnumerable<IMaterialLayer> Materials { get; }

        [CanBeNull]
        IMaterialLayer GetMaterialLayer(MaterialType type);
    }
}