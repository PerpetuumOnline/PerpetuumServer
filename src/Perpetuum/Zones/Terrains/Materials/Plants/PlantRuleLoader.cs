using System.Collections.Generic;
using System.IO;
using System.Linq;
using Perpetuum.Common;
using Perpetuum.Data;
using Perpetuum.Log;

namespace Perpetuum.Zones.Terrains.Materials.Plants
{
    public class PlantRuleLoader
    {
        private readonly SettingsLoader _settings;

        public PlantRuleLoader(SettingsLoader settings)
        {
            _settings = settings;
        }

        [NotNull]
        public List<PlantRule> LoadPlantRulesWithOverrides(int ruleSetId,double plantAltitudeScale = 1.0)
        {
            var ruleNames = Db.Query().CommandText("select plantrule from plantrules where rulesetid=@id")
                .SetParameter("@id",ruleSetId)
                .Execute()
                .Select(r => r.GetValue<string>(0)).ToArray();

            var list = new List<PlantRule>(ruleNames.Length);
            foreach (var ruleName in ruleNames)
            {
                var ruleDictionary = LoadRuleByName(ruleName);

                if (ruleDictionary.ContainsKey(k.allowedAltitudeLow))
                {
                    ruleDictionary[k.allowedAltitudeLow] = (int)((int)ruleDictionary[k.allowedAltitudeLow] * plantAltitudeScale);
                }

                if (ruleDictionary.ContainsKey(k.allowedAltitudeHigh))
                {
                    ruleDictionary[k.allowedAltitudeHigh] = (int)((int)ruleDictionary[k.allowedAltitudeHigh] * plantAltitudeScale);
                }

                if (ruleDictionary.ContainsKey(k.allowedWaterLevelLow))
                {
                    ruleDictionary[k.allowedWaterLevelLow] = (int)((int)ruleDictionary[k.allowedWaterLevelLow] * plantAltitudeScale);
                }

                list.Add(new PlantRule(ruleDictionary));
            }

            return list;
        }

        public IDictionary<string,object> LoadRuleByName(string ruleName)
        {
            //this is what we want to load
            var pathToFile = Path.Combine("plantrules",ruleName);

            //the content
            var settingsFromFile = _settings.LoadSettingsFromFile(pathToFile);

            if (!settingsFromFile.ContainsKey(k.source))
                return settingsFromFile;

            //is there override defined?
            var sourceRuleName = (string)settingsFromFile[k.source];

            Logger.Info("overwriting " + ruleName + " -> " + sourceRuleName);

            //load the override
            var sourceRuleFromFile = LoadRuleByName(sourceRuleName);

            //no need for this key in the memory
            settingsFromFile.Remove(k.source);

            foreach (var kvp in settingsFromFile)
            {
                //do override
                sourceRuleFromFile[kvp.Key] = kvp.Value;
            }

            settingsFromFile = sourceRuleFromFile;

            return settingsFromFile;
        }
    }
}