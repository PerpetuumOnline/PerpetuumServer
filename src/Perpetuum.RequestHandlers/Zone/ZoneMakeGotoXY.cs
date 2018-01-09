using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.IO;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneMakeGotoXY : IRequestHandler<IZoneRequest>
    {
        private readonly IZone _zone;
        private readonly IFileSystem _fileSystem;

        public ZoneMakeGotoXY(IZone zone,IFileSystem fileSystem)
        {
            _zone = zone;
            _fileSystem = fileSystem;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var definition = request.Data.GetOrDefault<int>(k.definition);

            var ed = EntityDefault.Get(definition);
            ed.Equals(EntityDefault.None).ThrowIfTrue(ErrorCodes.ItemNotFound);

            
            var units = _zone.Units.Where(u => u.ED.Definition == definition).ToArray();

            var linez = new List<string>();
            foreach (var unit in units)
            {
                var ux =  unit.CurrentPosition.intX;
                var uy = unit.CurrentPosition.intY;

                linez.Add("gotoxy " + ux + " " + uy);
            }

            _fileSystem.WriteAllLines("gotoxy_" + ed.Name + "_z" + _zone.Id + "_.txt",linez);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}
