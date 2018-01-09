using Perpetuum.Host.Requests;
using Perpetuum.Services.Insurance;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionSetInsurance : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            InsuranceHelper.LoadInsurancePrices();

            var result = InsuranceHelper.GetInsuranceState();

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}