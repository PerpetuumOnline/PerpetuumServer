using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
	/// <summary>
	/// 
	/// Returns the facilities of a docking base / all docking bases
	/// 
	/// all -> logintime
	/// single -> on successful dock in
	/// 
	/// </summary>
	public class BaseListFacilities : IRequestHandler
	{
	    private readonly IZoneManager _zoneManager;

	    public BaseListFacilities(IZoneManager zoneManager)
	    {
	        _zoneManager = zoneManager;
	    }

        public void HandleRequest(IRequest request)
		{
		    IEnumerable<Entity> children;

		    if (request.Data.ContainsKey(k.all))
		    {
		        children = _zoneManager.Zones.GetUnits<DockingBase>().SelectMany(b => b.Children);
		    }
            else
		    {
		        var eid = request.Data.GetOrDefault<long>(k.baseEID);
		        children = _zoneManager.GetUnit<DockingBase>(eid)?.Children ?? Enumerable.Empty<Entity>();
		    }

		    var childrenInfos = children.ToDictionary("i", child =>
		    {
		        switch (child)
		        {
                    case Market market:
                    {
                        return market.ToDictionary();
                    }
                    default:
                    {
                        return new Dictionary<string,object>(3)
                        {
                            [k.eid] = child.Eid,
                            [k.definition] = child.Definition,
                            [k.parent] = child.Parent
                        };
                    }
                }
		    });
		    Message.Builder.FromRequest(request).SetData(k.items,childrenInfos).Send();
		}
    }

}
