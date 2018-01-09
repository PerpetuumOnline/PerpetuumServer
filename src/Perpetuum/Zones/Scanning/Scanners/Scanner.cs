using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones.Scanning.Modules;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.Scanning.Scanners
{
    public partial class Scanner
    {
        private readonly IZone _zone;
        private readonly Player _player;
        private readonly GeoScannerModule _module;
        private readonly MaterialHelper _materialHelper;

        public delegate Scanner Factory(IZone zone, Player player, GeoScannerModule module);

        public Scanner(IZone zone,Player player,GeoScannerModule module,MaterialHelper materialHelper)
        {
            _zone = zone;
            _player = player;
            _module = module;
            _materialHelper = materialHelper;
        }

        private void OnMineralScanned(MaterialProbeType materialProbeType,MaterialType materialType = MaterialType.Undefined)
        {
            var m = _materialHelper.GetMaterialInfo(materialType);
            _player.MissionHandler.EnqueueMissionEventInfo(new ScanMaterialEventInfo(_player,m.EntityDefault.Definition,materialProbeType, _player.CurrentPosition )); 
        }
    }
}