using Perpetuum.Host.Requests;
using Perpetuum.Units.FieldTerminals;

namespace Perpetuum.RequestHandlers.Zone
{
    public class FieldTerminalInfo : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var eid = request.Data.GetOrDefault<long>(k.eid);
            var unit = request.Zone.GetUnit(eid).ThrowIfNull(ErrorCodes.ItemNotFound);

            if (!(unit is FieldTerminal fieldTerminal))
                throw new PerpetuumException(ErrorCodes.DefinitionNotSupported);

            var character = request.Session.Character;
            fieldTerminal.SendInfoToCharacter(character);
        }
    }
}