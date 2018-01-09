using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.FittingPreset
{
    public class FittingPresetList : FittingPresetRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            var forCorporation = request.Data.GetOrDefault<bool>(k.forCorporation);
            var character = request.Session.Character;
            var repo = GetFittingPresetRepository(character, forCorporation);
            SendAllPresetsToCharacter(request, repo);
        }
    }
}