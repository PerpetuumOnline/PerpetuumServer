using System.Collections.Generic;
using System.Linq;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers.FittingPreset
{
    public class FittingPresetApply : FittingPresetRequestHandler
    {
        private readonly IEntityRepository _entityRepository;
        private readonly RobotHelper _robotHelper;

        public FittingPresetApply(IEntityRepository entityRepository,RobotHelper robotHelper)
        {
            _entityRepository = entityRepository;
            _robotHelper = robotHelper;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();

                var character = request.Session.Character;
                var repo = GetFittingPresetRepository(character, forCorporation);
                var preset = repo.Get(id);
                var robot = _robotHelper.LoadRobotForCharacter(robotEid, character);
                robot.ED.ThrowIfNotEqual(preset.Robot, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                var container = Container.GetWithItems(containerEid, character);
                robot.EmptyRobot(character, container, false);
                robot.Initialize(character);

                foreach (var moduleInfos in preset.Modules.GroupBy(i => i.Component))
                {
                    var component = robot.GetRobotComponent((RobotComponentType) moduleInfos.Key).ThrowIfNull(ErrorCodes.ItemNotFound);

                    foreach (var moduleInfo in moduleInfos)
                    {
                        var module = container.GetItems().OfType<Module>().FirstOrDefault(m => m.ED == moduleInfo.Module);
                        if (module == null)
                            continue;

                        module = (Module)module.Unstack(1);

                        if (module is ActiveModule activeModule && moduleInfo.Ammo != EntityDefault.None)
                        {
                            var ammo = (Ammo)container.GetAndRemoveItemByDefinition(moduleInfo.Ammo.Definition, activeModule.AmmoCapacity);
                            if (ammo != null)
                                activeModule.SetAmmo(ammo);
                        }

                        component.EquipModuleOrThrow(module, moduleInfo.Slot);
                    }
                }

                robot.Initialize(character);

                robot.Save();
                container.Save();

                var result = new Dictionary<string, object>
                {
                    {k.robot, robot.ToDictionary()},
                    {k.container, container.ToDictionary()}
                };
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}