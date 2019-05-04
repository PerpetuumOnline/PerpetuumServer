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
        /*
        Language IDs:
            0 = English
            1 = Hungarian
            2 = German
            3 = Portuguese
            4 = Russian
            5 = French
            6 = Spanish
            7 = Polish
            8 = Slovenian
            9 = Romanian
            10 = Norwegian
            11 = Greek
            12 = Finnish
            13 = Italian
            14 = Turkish
            15 = Estonian
            16 = Swedish
            17 = Dutch
        */
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
