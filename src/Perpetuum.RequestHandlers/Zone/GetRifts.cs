using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Services.RiftSystem;

namespace Perpetuum.RequestHandlers.Zone
{
    public class GetRifts : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var rifts = request.Zone.Units.OfType<Rift>().ToList();

            var x = rifts.ToDictionary("r", r =>
            {
                var d = new Dictionary<string, object>
                {
                    [k.x] = (int) r.CurrentPosition.X,
                    [k.y] = (int) r.CurrentPosition.Y
                };

                var e = r.EffectHandler.GetEffectsByType(EffectType.effect_despawn_timer).FirstOrDefault();
                if (e != null)
                {
                    d[k.time] = (long)e.Timer.Remaining.TotalMilliseconds;
                }

                return d;
            });

            Message.Builder.FromRequest(request).WithData(x).Send();
        }
    }
}