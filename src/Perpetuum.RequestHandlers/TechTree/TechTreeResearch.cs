using System.Collections.Generic;
using System.Linq;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Services.TechTree;

namespace Perpetuum.RequestHandlers.TechTree
{
    public class TechTreeResearch : TechTreeRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var containerEid = request.Data.GetOrDefault<long>(k.container);
                var items = request.Data.GetOrDefault<long[]>(k.items);

                var character = request.Session.Character;
                var container = Container.GetWithItems(containerEid, character, ContainerAccess.Remove);
                var kernels = container.GetItems(items).OfType<Kernel>();
                var points = new TechTreePointsHandler(character.Eid);

                foreach (var kernelGroup in kernels.GroupBy(kernel => kernel.PointType))
                {
                    var pointType = kernelGroup.Key;
                    var sumQty = kernelGroup.Sum(kernel => kernel.Quantity);

                    points.UpdatePoints(pointType, current => current + sumQty);

                    foreach (var kernel in kernelGroup)
                    {
                        Entity.Repository.Delete(kernel);
                    }

                    var logEvent = new LogEvent(LogType.Research, character)
                    {
                        Definition = kernelGroup.First().Definition,
                        Quantity = sumQty,
                        Points = new Points(pointType, sumQty)
                    };

                    TechTreeLogger.WriteLog(logEvent);
                }

                var result = new Dictionary<string, object>
                {
                    {k.container, container.ToDictionary()},
                };

                points.AddAvailablePointsToDictionary(result);
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}