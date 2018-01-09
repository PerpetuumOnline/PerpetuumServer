using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Players;
using Perpetuum.Zones;
using Perpetuum.Zones.Scanning.Modules;
using Perpetuum.Zones.Scanning.Results;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneUploadScanResult : IRequestHandler<IZoneRequest>
    {
        private readonly MineralScanResultRepository.Factory _scanResultRepositoryFactory;

        public ZoneUploadScanResult(MineralScanResultRepository.Factory scanResultRepositoryFactory)
        {
            _scanResultRepositoryFactory = scanResultRepositoryFactory;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                request.Zone.TryGetPlayer(character, out Player player).ThrowIfFalse(ErrorCodes.PlayerNotFound);

                var scannerModule = player.Modules.OfType<GeoScannerModule>().FirstOrDefault().ThrowIfNull(ErrorCodes.GeoScannerModuleNotFound);
                var scanResult = scannerModule.LastScanResult.ThrowIfNull(ErrorCodes.ScanResultNotFound);

                var repo = _scanResultRepositoryFactory(character);
                repo.InsertOrThrow(scanResult);

                Transaction.Current.OnCommited(() =>
                {
                    // ha minden ok akkor kitoroljuk az utolso scanneles eredemenyet,h ne duplikalodjon az sql-be
                    scannerModule.LastScanResult = null;
                    // szepen kozoljuk a klienssel,h minden rendben van
                    Message.Builder.FromRequest(request).WithData(scanResult.ToDictionary()).Send();
                });
                
                scope.Complete();
            }
        }
    }
}
