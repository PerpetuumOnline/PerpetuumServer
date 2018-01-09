using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkList : IRequestHandler
    {
        private readonly SparkHelper _sparkHelper;

        public SparkList(SparkHelper sparkHelper)
        {
            _sparkHelper = sparkHelper;
        }

        public void HandleRequest(IRequest request)
        {
            _sparkHelper.SendSparksList(request);
        }
    }
}