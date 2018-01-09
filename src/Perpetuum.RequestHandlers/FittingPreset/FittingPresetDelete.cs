using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.FittingPreset
{
    public class FittingPresetDelete : FittingPresetRequestHandler
    {
        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();

                var character = request.Session.Character;
                var repo = GetFittingPresetRepository(character, forCorporation);
                repo.DeleteById(id);

                SendAllPresetsToCharacter(request, repo);
                scope.Complete();
            }
        }
    }
}