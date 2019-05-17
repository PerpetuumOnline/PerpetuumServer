using Newtonsoft.Json;
using Perpetuum.Log;
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

        public CustomDictionary(IO.IFileSystem fileManager)
        {
            _dictionaries = new Dictionary<int, Dictionary<string, object>>();

            // if the directory doesn't exist or the JSON is malformed just throw an exception, log it and move on.
            // Otherwise we get a blank screen on the client.
            try
            {                
                var files = fileManager.GetFiles("customDictionary", "*.json");

                foreach (var file in files)
                {
                    var languageID = Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(file));
                    var settingsFile = fileManager.ReadAllText(file);
                    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(settingsFile);
                    _dictionaries.Add(languageID, dictionary);
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public Dictionary<string, object> GetDictionary(int language)
        {
            if (_dictionaries.ContainsKey(language))
            {
                return _dictionaries[language];
            }
            else if (_dictionaries.ContainsKey(_defaultLanguage))
            {
                return _dictionaries[_defaultLanguage];
            }
            return null;
        }
    }
}