using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class CharacterCorporationHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));

            Message.Builder.FromRequest(request)
                .WithData(Corporation.GetCorporationHistory(character))
                .WrapToResult()
                .Send();
        }
    }
}