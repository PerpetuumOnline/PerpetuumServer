using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum
{
    public interface ICustomDictionary
    {
        Dictionary<string, object> GetDictionary(int language);
    }

    public class CustomDictionary : ICustomDictionary
    {
        private readonly int _defaultLanguage = 0;

        public Dictionary<int, Dictionary<string, object>> _dictionaries;
        public CustomDictionary(Perpetuum.IO.IFileSystem fileManager)
        {
            _dictionaries = new Dictionary<int, Dictionary<string, object>>();
            var files = fileManager.GetFiles("customDictionary", "*.json");

            foreach (var file in files)
            {
                var languageID = Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(file));
                var settingsFile = fileManager.ReadAllText(file);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(settingsFile);
                _dictionaries.Add(languageID, dictionary);
            }
        }

        public Dictionary<string, object> GetDictionary(int language)
        {
            if (_dictionaries.ContainsKey(language))
            {
                return _dictionaries[language];
            }
            if (_dictionaries.ContainsKey(_defaultLanguage))
            {
                return _dictionaries[_defaultLanguage];
            }
            return null;
        }
    }
}
