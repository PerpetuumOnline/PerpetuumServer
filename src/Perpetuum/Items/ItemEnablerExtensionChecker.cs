using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Modules;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.Items
{
    public class ItemEnablerExtensionChecker : IEntityVisitor<Item>,IEntityVisitor<ActiveModule>,IEntityVisitor<Robot>
    {
        private readonly CharacterExtensionCollection _characterExtensions;
        private readonly HashSet<Extension> _missingExtensions = new HashSet<Extension>();

        private ItemEnablerExtensionChecker(Character character)
        {
            _characterExtensions = character.GetExtensions();
        }

        private void CheckItemEnablerExtensions(Item item)
        {
            var enablerExtensions = item.ED.EnablerExtensions;
            CheckExtensions(enablerExtensions.Keys.Concat(enablerExtensions.SelectMany(e => e.Value)));
        }

        public void Visit(Item item)
        {
            CheckItemEnablerExtensions(item);
        }

        public void Visit(ActiveModule module)
        {
            CheckItemEnablerExtensions(module);
            module.VisitAmmo(this);
        }

        public void Visit(Robot robot)
        {
            CheckItemEnablerExtensions(robot);
            CheckExtensions(robot.ExtensionBonusEnablerExtensions);
            robot.VisitModules(this);
        }

        private void CheckExtensions(IEnumerable<Extension> extensions)
        {
            _missingExtensions.AddIf(extensions, ex => ex.level > _characterExtensions.GetLevel(ex.id));
        }

        public static Extension[] Check(Item item, Character character)
        {
            if (character == Character.None)
                return new Extension[0];

            var checker = new ItemEnablerExtensionChecker(character);
            item.AcceptVisitor(checker);
            return checker._missingExtensions.ToArray();
        }

    }
}
