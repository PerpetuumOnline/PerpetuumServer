using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perpetuum.GenXY;
using Perpetuum.IO;
using Perpetuum.Log;

namespace Perpetuum.Common
{
    public class SettingsLoader
    {
        private readonly IFileSystem _fileSystem;

        public SettingsLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Loads an .ini file and returns a dictionary
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [NotNull]
        public IDictionary<string, object> LoadSettingsFromFile(string path)
        {
            if (!_fileSystem.Exists(path))
            {
                Logger.Warning($"ini file not found: {path}");
                return new Dictionary<string, object>();
            }

            var lines = _fileSystem.ReadAllText(path).GetLines().Select(l => l.RemoveComment()).Where(line => line.Length > 0).ToArray();

            var strSettings = new StringBuilder();

            foreach (var line in lines)
            {
                strSettings.Append(line);
                strSettings.Append('#');
            }

            var result = GenxyConverter.Deserialize(strSettings.ToString());
            return result;
        }
    }
}
