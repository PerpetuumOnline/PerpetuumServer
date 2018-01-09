using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Players;

namespace Perpetuum.Items
{
    public static class ItemExtensions
    {
        public static void Initialize(this Item item, Character character)
        {
            if (item is Player player)
            {
                player.Character = character;
            }

            item.Initialize();

            foreach (var p in item.Children.OfType<Item>())
            {
                p.Initialize(character);
            }
        }

        [NotNull]
        public static Character GetCharacter(this Item item)
        {
            var player = item as Player;
            return player?.Character ?? Character.None;
        }

        public static Character GetOwnerAsCharacter(this Item entity)
        {
            return Character.GetByEid(entity.Owner);
        }

        public static void StackMany(this IEnumerable<Item> items)
        {
            var stackableGroup = items.Where(i => i.IsStackable).GroupBy(i => i.Definition);

            foreach (var itemsGroups in stackableGroup)
            {
                foreach (var itemsGroup in itemsGroups.GroupBy(i => i.IsRepackaged))
                {
                    var masterItem = itemsGroup.First();

                    foreach (var item in itemsGroup.Where(i => i != masterItem))
                    {
                        try
                        {
                            item.StackToOrThrow(masterItem);
                        }
                        catch (PerpetuumException gex)
                        {
                            item.SendErrorMessageToOwner(Commands.StackItems, gex.error);
                            throw;
                        }
                    }
                }
            }
        }

        public static void PackMany(this IEnumerable<Item> items)
        {
            foreach (var item in items)
            {
                try
                {
                    item.Pack();
                }
                catch (PerpetuumException gex)
                {
                    item.SendErrorMessageToOwner(Commands.PackItems, gex.error);
                    throw;
                }
            }
        }

        public static void UnpackMany(this IEnumerable<Item> items)
        {
            using (var n = new ItemErrorNotifier(true))
            {
                foreach (var item in items)
                {
                    try
                    {
                        item.Unpack();
                    }
                    catch (PerpetuumException gex)
                    {
                        n.AddError(item,gex);
                    }
                }
            }
        }

        public static void Pack(this Item item)
        {
            ItemPacker.Pack(item);
        }

        public static void Unpack(this Item item)
        {
            ItemUnpacker.Unpack(item);
        }

        public static void Repair(this Item item)
        {
            ItemRepairer.Repair(item);
        }

        public static bool HaveAllEnablerExtensions(this Item item,Character character)
        {
            var missingEnablerExtensions = ItemEnablerExtensionChecker.Check(item, character);
            return missingEnablerExtensions.Length == 0;
        }

        public static void CheckEnablerExtensionsAndThrowIfFailed(this Item item,Character character, ErrorCodes error = ErrorCodes.ExtensionLevelMismatch)
        {
            var missingEnablerExtensions = ItemEnablerExtensionChecker.Check(item, character);
            missingEnablerExtensions.Length.ThrowIfGreater(0, error, gex => gex.SetData("missingEnablerExtensions", missingEnablerExtensions.ToDictionary("e", m => m.ToDictionary())));
        }

        public static void SendErrorMessageToOwner(this Item item,Command command, ErrorCodes error)
        {
            item.GetOwnerAsCharacter().SendItemErrorMessage(command, error, item);
        }
    }
}