using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Robots.Fitting;

namespace Perpetuum.RequestHandlers.FittingPreset
{
    public abstract class FittingPresetRequestHandler : IRequestHandler
    {
        public abstract void HandleRequest(IRequest request);

        protected static void SendAllPresetsToCharacter(IRequest request, IFittingPresetRepository repo)
        {
            void Sender()
            {
                var result = repo.GetAll().ToDictionary("p", p => p.ToDictionary());
                Message.Builder.FromRequest(request).WithData(result).Send();
            };

            if (Transaction.Current != null)
            {
                Transaction.Current.OnCommited(Sender);
            }
            else
                Sender();
        }

        protected static IFittingPresetRepository GetFittingPresetRepository(Character character, bool forCorporation)
        {
            if (!forCorporation)
                return new CharacterFittingPresetRepository(character);

            var corporation = character.GetPrivateCorporationOrThrow();
            return new CorporationFittingPresetRepository(corporation);
        }
    }
}