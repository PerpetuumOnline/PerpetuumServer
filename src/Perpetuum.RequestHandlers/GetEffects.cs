using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Effects;

namespace Perpetuum.RequestHandlers
{
    public class GetEffects : IRequestHandler
    {
        private readonly Dictionary<string, object> _effectInfos;

        public GetEffects()
        {
            var effects = EffectHelper.GetEffectInfosDictionary();
            var effectDefaultModifiers = EffectHelper.GetEffectDefaultModifiersDictionary();
            _effectInfos = new Dictionary<string, object>
            {
                {"effects", effects},
                {"effectDefaultModifiers", effectDefaultModifiers}
            };
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_effectInfos).Send();
        }
    }
}