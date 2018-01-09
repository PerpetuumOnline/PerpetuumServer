using Perpetuum.Host.Requests;
using Perpetuum.Services.Insurance;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionInsuranceList : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            InsuranceHelper.SendInsuranceListToCharacter(character);
        }
    }
}