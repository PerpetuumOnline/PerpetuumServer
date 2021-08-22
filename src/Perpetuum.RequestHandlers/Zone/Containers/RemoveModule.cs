using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Players;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class RemoveModule : ZoneChangeModule
    {
        public override void DoChange(IZoneRequest request, Player player, Container container)
        {
            var moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
            var module = player.GetModule(moduleEid).ThrowIfNull(ErrorCodes.ModuleNotFound);
            player.CheckEnergySystemAndThrowIfFailed(module, true);
            module.Owner = player.Character.Eid;
            module.Unequip(container);
        }
    }
}