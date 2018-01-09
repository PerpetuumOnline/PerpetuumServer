using Perpetuum.Host.Requests;
using Perpetuum.Services.Insurance;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionGetInsurance : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request)
                .WithData(InsuranceHelper.GetInsuranceState())
                .Send();
        }
    }
}