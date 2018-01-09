using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketRemoveItems : IRequestHandler
    {
        private readonly IEntityServices _entityServices;
        private readonly MarketHelper _marketHelper;

        public MarketRemoveItems(IEntityServices entityServices,MarketHelper marketHelper)
        {
            _entityServices = entityServices;
            _marketHelper = marketHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var cfLong = request.Data.GetOrDefault<long>(k.categoryFlags);
                var category = request.Data.GetOrDefault<string>(k.category);
                var global = request.Data.GetOrDefault<int>(k.global) == 1;
                var marketEID = request.Data.GetOrDefault<long>(k.marketEID);
                var withVendor = request.Data.GetOrDefault("vendor", 1) == 1;
                var nameFilter = request.Data.GetOrDefault<string>(k.filter);

                if (global)
                {
                    if (cfLong > 0)
                    {
                        _marketHelper.RemoveItemsByCategoryFlags((CategoryFlags)cfLong, withVendor);
                    }

                    if (Enum.TryParse(category, true, out CategoryFlags cf))
                    {
                        _marketHelper.RemoveItemsByCategoryFlags(cf, withVendor);
                    }
                }
                else
                {
                    if (marketEID == 0)
                    {
                        var character = request.Session.Character;
                        var dockingBase = character.GetCurrentDockingBase();
                        var market = dockingBase.GetMarket();
                        marketEID = market.Eid;

                    }

                    Enum.TryParse(category, true, out CategoryFlags cf).ThrowIfFalse(ErrorCodes.SyntaxError);

                    var definitions = _entityServices.Defaults.GetAll().GetDefinitionsByCategoryFlag(cf);

                    if (!nameFilter.IsNullOrEmpty())
                    {
                        var tmpList = new List<int>();

                        foreach (var definition in definitions)
                        {
                            var ed = EntityDefault.Get(definition);

                            if (!ed.Name.Contains(nameFilter))
                                continue;

                            tmpList.Add(ed.Definition);
                        }

                        definitions = tmpList.ToArray();
                    }

                    var definitionsString = definitions.ArrayToString();

                    var queryString = "delete marketitems where marketeid=@marketEID and itemdefinition in (" + definitionsString + ") ";

                    if (!withVendor)
                    {
                        queryString += " and isvendoritem=0";
                    }

                    Db.Query().CommandText(queryString)
                        .SetParameter("@marketEID", marketEID)
                        .ExecuteNonQuery();


                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}