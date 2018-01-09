using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;

namespace Perpetuum.RequestHandlers.Production
{
    public class ProductionLineSetRounds : IRequestHandler
    {
        private readonly ProductionManager _productionManager;

        public ProductionLineSetRounds(ProductionManager productionManager)
        {
            _productionManager = productionManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var lineId = request.Data.GetOrDefault<int>(k.ID);
                var rounds = request.Data.GetOrDefault<int>(k.rounds);
                var facilityEid = request.Data.GetOrDefault<long>(k.facility);

                _productionManager.GetFacilityAndCheckDocking(facilityEid, character, out Mill mill);

                ProductionLine.LoadById(lineId, out ProductionLine productionLine).ThrowIfError();

                productionLine.CharacterId.ThrowIfNotEqual(character.Id, ErrorCodes.OwnerMismatch);

                var maxRounds = Mill.GetMaxRounds(character);

                if (rounds > maxRounds)
                    rounds = maxRounds;

                ProductionLine.SetRounds(rounds, productionLine.Id).ThrowIfError();

                var linesList = mill.GetLinesList(character);
                var facilityInfo = mill.GetFacilityInfo(character);

                var reply = new Dictionary<string, object>
                {
                    {k.lineCount, linesList.Count},
                    {k.lines, linesList},
                    {k.facility, facilityInfo}
                };

                Message.Builder.SetCommand(Commands.ProductionLineList).WithData(reply).ToClient(request.Session).Send();
                
                scope.Complete();
            }
        }
    }
}