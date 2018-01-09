using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers
{
    public class GetEntityDefaults : IRequestHandler
    {
        private readonly IMessage _message;

        public GetEntityDefaults(IExtensionReader extensionReader,IEntityDefaultReader entityDefaultReader,IRobotTemplateRelations robotTemplateRelations)
        {
            var cachedEntityDefaultsInfo = entityDefaultReader.GetAll().ToDictionary("d",ed => InfoBuilder.Build(extensionReader,robotTemplateRelations,ed));
            _message = Message.Builder.SetCommand(Commands.GetEntityDefaults).WithData(cachedEntityDefaultsInfo).Build();
        }

        public void HandleRequest(IRequest request)
        {
            request.Session.SendMessage(_message);
        }

        private class InfoBuilder : IEntityVisitor<Entity>,IEntityVisitor<Item>,IEntityVisitor<Module>,IEntityVisitor<Robot>,IEntityVisitor<RobotComponent>
        {
            private readonly IExtensionReader _extensionReader;
            private readonly Dictionary<string, object> _info;

            private InfoBuilder(IExtensionReader extensionReader,Dictionary<string,object> info)
            {
                _extensionReader = extensionReader;
                _info = info;
            }

            private void AddEntityInfo(Entity entity)
            {
                _info[k.volume] = entity.ED.Volume;
                _info[k.repackedvolume] = entity.ED.Volume * 0.5;
                _info[k.mass] = entity.Mass;
            }

            private void AddItemInfo(Item item)
            {
                AddEntityInfo(item);
                item.AddPropertiesToDictionary(_info);
            }

            public void Visit(Entity entity)
            {
                AddEntityInfo(entity);
            }

            public void Visit(Item item)
            {
                AddItemInfo(item);
            }

            public void Visit(Module module)
            {
                AddItemInfo(module);

                var extensionIds = module.GetPropertyModifiers()
                         .Distinct() // csak egyet
                         .Select(f => _extensionReader.GetExtensionsByAggregateField(f)) // elkerjuk az osszes extensiont ami erre a fieldre hat
                         .SelectMany(infos => infos)
                         .Select(ex => ex.id)
                         .OrderBy(id => id).ToArray();

                if (extensionIds.Length > 0)
                {
                    _info[k.extension] = extensionIds;
                }
            }

            public void Visit(Robot robot)
            {
                AddItemInfo(robot);

                var container = robot.GetContainer();
                if (container != null)
                    _info.Add(k.container, container.Definition);

                _info[k.volume] = robot.Volume;
                _info[k.repackedvolume] = robot.Volume * 0.5;
            }

            public void Visit(RobotComponent component)
            {
                AddItemInfo(component);

                var bonus = component.ExtensionBonuses.ToDictionary("a", cb => cb.ToDictionary());
                if (bonus.Count > 0)
                    _info[k.bonus] = bonus;
            }

            public static Dictionary<string, object> Build(IExtensionReader extensionReader,IRobotTemplateRelations robotTemplateRelations,EntityDefault ed)
            {
                var info = ed.ToDictionary();

                try
                {
                    Entity entity = null;

                    var robotTemplate = robotTemplateRelations.GetRelatedTemplate(ed);
                    if (robotTemplate != null)
                        entity = robotTemplate.Build();

                    if (entity == null)
                    {
                        entity = Entity.Factory.CreateWithRandomEID(ed);
                    }

                    var item = entity as Item;
                    item?.Initialize();

                    var builder = new InfoBuilder(extensionReader,info);
                    entity.AcceptVisitor(builder);
                }
                catch (Exception ex)
                {
                    Logger.Error($"troubled definition: {ed.Definition}  {ex.Message}");
                    Logger.Error($"{ex}\n{ex.Message}\n{ex.Source}\n{ex.InnerException?.Message}\n{ex.StackTrace}\n");
                }

                return info;
            }

        }
    }
}