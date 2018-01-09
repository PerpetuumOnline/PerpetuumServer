using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Services.ExtensionService
{
    public class CharacterDefaultExtensionHelper
    {
        private readonly IEnumerable<Extension> _extensions; 

        public CharacterDefaultExtensionHelper(Character character)
        {
            _extensions = character.GetDefaultExtensions();
        }

        public bool IsStartingExtension(ExtensionInfo extension, out int minimumLevel)
        {
            var tmpExtensoin = new Extension(extension.id, 0);
            return IsStartingExtension(tmpExtensoin, out minimumLevel);
        }

        public bool IsStartingExtension(Extension extension, out int minimumLevel)
        {
            foreach (var starterExtension in _extensions)
            {
                if (extension.id != starterExtension.id)
                    continue;
                minimumLevel = starterExtension.level;
                return true;
            }

            minimumLevel = 0;
            return false;
        }
    }
}
