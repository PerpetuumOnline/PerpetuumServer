using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Standings
{
    public class ForceStanding : IRequestHandler
    {
        private readonly IStandingHandler _standingHandler;

        public ForceStanding(IStandingHandler standingHandler)
        {
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var source = request.Data.GetOrDefault<long>(k.source);
                var target = request.Data.GetOrDefault<long>(k.target);
                var value = request.Data.GetOrDefault<double>(k.standing);

                _standingHandler.SetStanding(source, target, value);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}