using System;
using System.Globalization;
using System.Runtime.Caching;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.ExtensionService
{
    /// <summary>
    /// Extension cache
    /// </summary>
    public class CharacterExtensions : ICharacterExtensions
    {
        private readonly IExtensionReader _extensionReader;
        private readonly ObjectCache _extensions;

        public CharacterExtensions(IExtensionReader extensionReader,Func<string,ObjectCache> cacheFactory)
        {
            _extensionReader = extensionReader;
            _extensions = cacheFactory("Extensions");
        }

        public CharacterExtensionCollection Get(Character character)
        {
            if (character == Character.None)
                return CharacterExtensionCollection.None;

            return _extensions.Get(character.Id.ToString(CultureInfo.InvariantCulture), () => new CharacterExtensionCollection(_extensionReader,character), TimeSpan.FromHours(1));
        }

        public void Remove(Character character)
        {
            Clear(character);
        }

        private void Clear(Character character)
        {
            _extensions.Remove(character.Id.ToString(CultureInfo.InvariantCulture));
        }
    }
}
