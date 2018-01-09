using System.Collections.Generic;
using System.Globalization;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Timers;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionTest : IRequestHandler
    {
        private readonly GiveExtensionPointsService _giveExtensionPointsService;

        public ExtensionTest(GiveExtensionPointsService giveExtensionPointsService)
        {
            _giveExtensionPointsService = giveExtensionPointsService;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var before = GlobalTimer.Elapsed;

                var sqlTime = _giveExtensionPointsService.DoGiveExtensionPointsToAccounts();

                _giveExtensionPointsService.InformAffectedCharacters(sqlTime);

                var later = GlobalTimer.Elapsed;

                var diff = later - before;

                var d = new Dictionary<string, object>
                {
                    {k.time, diff.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}
                };

                Message.Builder.FromRequest(request).WithData(d).Send();
                
                scope.Complete();
            }
        }
    }
}