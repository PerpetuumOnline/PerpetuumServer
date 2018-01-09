using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.IO;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketAddCategory : IRequestHandler
    {
        private readonly IFileSystem _fileSystem;

        public MarketAddCategory(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var vendorEID = request.Data.GetOrDefault<long>(k.vendorEID);
                var marketEID = request.Data.GetOrDefault<long>(k.marketEID);
                var isSell = request.Data.GetOrDefault<int>(k.isSell) == 1;
                var quantity = request.Data.GetOrDefault<int>(k.quantity);
                var clear = request.Data.GetOrDefault<string>(k.clear); //none, sell, buy, both
                var duration = request.Data.GetOrDefault<int>(k.duration);
                var price = (long)request.Data.GetOrDefault<int>(k.price); //!! >> defined as int to avoid hex conversion

                var category = request.Data.GetOrDefault<string>(k.category); //optional
                var fileName = request.Data.GetOrDefault<string>(k.file); //optional
                var addNamed = request.Data.GetOrDefault<int>(k.addNamed) == 1;
                var nameFilter = request.Data.GetOrDefault<string>(k.filter);

                Market market;

                if (vendorEID == 0 || marketEID == 0)
                {
                    var character = request.Session.Character;
                    var dockingBase = character.GetCurrentDockingBase();

                    market = dockingBase.GetMarket();
                    vendorEID = market.GetVendorEid();
                }
                else
                {
                    market = Market.GetOrThrow(marketEID);
                }

                //do clear
                switch (clear)
                {
                    case "sell":
                        Market.ClearVendorItems(vendorEID, true);
                        break;
                    case "buy":
                        Market.ClearVendorItems(vendorEID, false);
                        break;
                    case "both":
                        Market.ClearVendorItems(vendorEID, true);
                        Market.ClearVendorItems(vendorEID, false);
                        break;
                }

                if (category != null)
                {
                    //category flag defined as string
                    market.AddCategoryToMarket(vendorEID, category, duration, price, isSell, quantity, addNamed, nameFilter);
                }

                if (fileName != null)
                {
                    var linez = _fileSystem.ReadAllLines(fileName);
                    var trimmed = from l in linez select l.Trim();

                    foreach (var categoryName in trimmed)
                    {
                        market.AddCategoryToMarket(vendorEID, categoryName, duration, price, isSell, quantity, addNamed, nameFilter);
                    }
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}