using Perpetuum.Containers;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers.Zone.Containers
{
    public class EquipModule : ZoneChangeModule
    {
        public override void DoChange(IZoneRequest request, Player player, Container container)
        {
            var componentType = request.Data.GetOrDefault<string>(k.robotComponent).ToEnum<RobotComponentType>();
            var component = player.GetRobotComponentOrThrow(componentType);
            var slot = request.Data.GetOrDefault<int>(k.slot);
            component.MakeSlotFree(slot, container);

            var moduleEid = request.Data.GetOrDefault<long>(k.moduleEID);
            var module = (Module)container.GetItemOrThrow(moduleEid).Unstack(1);
            module.CheckEnablerExtensionsAndThrowIfFailed(player.Character);
            component.EquipModuleOrThrow(module, slot);
        }
    }
}