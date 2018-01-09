using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.RequestHandlers
{
    public class TeleportConnectColumns : IRequestHandler
    {
        private readonly ITeleportDescriptionRepository _teleportDescriptionRepository;
        private readonly TeleportDescriptionBuilder.Factory _descriptionBuilderFactory;

        public TeleportConnectColumns(ITeleportDescriptionRepository teleportDescriptionRepository,TeleportDescriptionBuilder.Factory descriptionBuilderFactory)
        {
            _teleportDescriptionRepository = teleportDescriptionRepository;
            _descriptionBuilderFactory = descriptionBuilderFactory;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sourceEid = request.Data.GetOrDefault<long>(k.source);
                var targetEid = request.Data.GetOrDefault<long>(k.target);
                var bothWays = request.Data.GetOrDefault<int>(k.both) == 1;

                ConnectColumns(sourceEid, targetEid);

                if (bothWays)
                {
                    ConnectColumns(targetEid, sourceEid);
                }

                Transaction.Current.OnCommited(() =>
                {
                    var result = _teleportDescriptionRepository.GetAll().ToDictionary();
                    Message.Builder.SetCommand(Commands.TeleportList)
                        .WithData(result)
                        .ToClient(request.Session)
                        .Send();
                });
                
                scope.Complete();
            }
        }

        private void ConnectColumns(long sourceEid, long targetEid)
        {
            var builder = _descriptionBuilderFactory();
            builder.SetSourceTeleport(sourceEid)
                   .SetTargetTeleport(targetEid)
                   .SetDescription($"{builder.SourceTeleport.Name}_to_{builder.TargetTeleport.Name}")
                   .SelectTypeByZones();
            var td = builder.Build();
            _teleportDescriptionRepository.Insert(td);
        }
    }
}