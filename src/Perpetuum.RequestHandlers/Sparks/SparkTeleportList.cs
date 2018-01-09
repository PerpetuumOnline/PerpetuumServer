using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks.Teleports;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkTeleportList : IRequestHandler
    {
        private readonly SparkTeleportHelper _sparkTeleportHelper;

        public SparkTeleportList(SparkTeleportHelper sparkTeleportHelper)
        {
            _sparkTeleportHelper = sparkTeleportHelper;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var info = _sparkTeleportHelper.GetSparkTeleportDescriptionInfos(character);

            info.Add(k.fee,SparkTeleport.SPARK_TELEPORT_USE_FEE);
            info.Add("placeFee",SparkTeleport.SPARK_TELEPORT_PLACE_FEE);

            Message.Builder.FromRequest(request).WithData(info).Send();
        }
    }
}