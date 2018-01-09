using System;
using Perpetuum.IDGenerators;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Modules;
using Perpetuum.Robots;

namespace Perpetuum.EntityFramework
{
    public class EntityFactory : IEntityFactory
    {
        private readonly Func<EntityDefault, Entity> _factory;
        private readonly IEntityDefaultReader _defaultReader;
        private readonly DefaultPropertyModifierReader _defaultPropertyModifierReader;
        private readonly RobotTemplateFactory _robotTemplateFactory;
        private readonly ModulePropertyModifiersReader _modulePropertyModifiersReader;

        public EntityFactory(Func<EntityDefault,Entity> factory,IEntityDefaultReader defaultReader,DefaultPropertyModifierReader defaultPropertyModifierReader,RobotTemplateFactory robotTemplateFactory,ModulePropertyModifiersReader modulePropertyModifiersReader)
        {
            _factory = factory;
            _defaultReader = defaultReader;
            _defaultPropertyModifierReader = defaultPropertyModifierReader;
            _robotTemplateFactory = robotTemplateFactory;
            _modulePropertyModifiersReader = modulePropertyModifiersReader;
        }

        public Entity Create(string definitionName,IIDGenerator<long> idGenerator)
        {
            return Create(_defaultReader.GetByName(definitionName),idGenerator);
        }

        public Entity Create(int definition,IIDGenerator<long> idGenerator)
        {
            var ed = _defaultReader.Get(definition);
            return Create(ed,idGenerator);
        }

        public Entity Create(EntityDefault entityDefault,IIDGenerator<long> idGenerator)
        {
            var entity = _factory(entityDefault);
            entity.Eid = idGenerator.GetNextID();


            if (entity is Item item)
            {
                var modifiers = _defaultPropertyModifierReader.GetByDefinition(entityDefault.Definition);
                item.BasePropertyModifiers = new PropertyModifierCollection(modifiers);

                if (item is Module module)
                {
                    module.PropertyModifiers = _modulePropertyModifiersReader.GetModifiers(module);
                }

                if (item is Robot robot)
                {
                    robot.Template = _robotTemplateFactory(entityDefault.Definition);

                    if (!robot.IsRepackaged)
                    {
                        robot.CreateComponents();
                    }
                }

                item.Initialize();
            }

            return entity;
        }
    }
}

