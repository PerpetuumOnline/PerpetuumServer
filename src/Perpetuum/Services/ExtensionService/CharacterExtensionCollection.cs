using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.ExtensionService
{
    public class CharacterExtensionCollection : IEnumerable<Extension>
    {
        public static readonly CharacterExtensionCollection None = new CharacterExtensionCollection();
        private readonly Extension[] _extensions;

        private CharacterExtensionCollection()
        {
            _extensions = new Extension[0];  
        }

        public CharacterExtensionCollection(IExtensionReader extensionReader,Character character)
        {
            _extensions = GetAll(extensionReader,character);
        }

        private Extension[] GetAll(IExtensionReader extensionReader,Character character)
        {
            return Db.Query().CommandText("select extensionid,extensionlevel from characterextensions where characterid = @characterId")
                .SetParameter("@characterId", character.Id)
                .Execute()
                .Select(r => new Extension(r.GetValue<int>(0), r.GetValue<int>(1)))
                .Where(ex => extensionReader.GetExtensions().ContainsKey(ex.id))
                .ToArray();
        }

        public IEnumerable<Extension> SelectById(IEnumerable<int> extensionIds)
        {
            foreach (var extensionId in extensionIds)
            {
                Extension extension;
                if (TryGet(extensionId, out extension))
                {
                    yield return extension;
                }
            }
        }

        public bool TryGet(int extensionId, out Extension extension)
        {
            extension = _extensions.FirstOrDefault(e => e.id == extensionId);
            return extension.id != 0;
        }

        public int GetLevel(int extensionId)
        {
            var extension = _extensions.FirstOrDefault(e => e.id == extensionId);
            return extension.level;
        }

        public IEnumerator<Extension> GetEnumerator()
        {
            return _extensions.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}