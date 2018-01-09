using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.ExtensionService
{
    public interface ICharacterExtensions
    {
        CharacterExtensionCollection Get(Character character);
        void Remove(Character character);
    }
}