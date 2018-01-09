using System.Collections.Immutable;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.ExtensionService
{
    public interface IExtensionReader
    {
        Extension[] GetEnablerExtensions(int definition);
        ImmutableDictionary<int, ExtensionInfo> GetExtensions();
        Extension[] GetCharacterDefaultExtensions(Character character);
        ExtensionBonus[] GetRobotComponentExtensionBonus(int robotComponentDefinition);
    }
}