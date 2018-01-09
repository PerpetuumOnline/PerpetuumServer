using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks.Teleports;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkTeleportDelete : IRequestHandler
    {
        private readonly SparkTeleportHelper _sparkTeleportHelper;

        public SparkTeleportDelete(SparkTeleportHelper sparkTeleportHelper)
        {
            _sparkTeleportHelper = sparkTeleportHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);

                var character = request.Session.Character;

                var teleport = _sparkTeleportHelper.Get(id);
                if (teleport.Character != character)
                    return;

                _sparkTeleportHelper.DeleteAndInform(teleport);

                Transaction.Current.OnCommited(() =>
                {
                    var info = _sparkTeleportHelper.GetSparkTeleportDescriptionInfos(character);
                    Message.Builder.FromRequest(request).WithData(info).Send();
                });
                
                scope.Complete();
            }
        }
    }
}