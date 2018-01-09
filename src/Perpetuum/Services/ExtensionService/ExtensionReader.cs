using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Services.ExtensionService
{
    public class ExtensionReader : IExtensionReader
    {
        private readonly Lazy<IEntityDefaultReader> _entityDefaultReader;
        private ILookup<int, Extension> _enablerExtensions;
        private ImmutableDictionary<int, ExtensionInfo> _extensions;
        private ILookup<int, ExtensionBonus> _robotComponentExtensionBonuses;

        public ExtensionReader(Lazy<IEntityDefaultReader> entityDefaultReader)
        {
            _entityDefaultReader = entityDefaultReader;
        }

        private ILookup<int, Extension> GetEnablerExtensions()
        {
            if (_enablerExtensions == null)
            {
                var extensions = GetExtensions();

                _enablerExtensions = Db.Query().CommandText("select * from enablerextensions")
                    .Execute()
                    .Select(r => new
                    {
                        definition = r.GetValue<int>("definition"),
                        extension = new Extension(r.GetValue<int>("extensionid"), r.GetValue<int>("extensionlevel"))
                    })
                    .Where(x => extensions.ContainsKey(x.extension.id))
                    .Distinct()
                    .ToLookup(x => x.definition, x => x.extension);
            }

            return _enablerExtensions;
        }

        public Extension[] GetEnablerExtensions(int definition)
        {
            return GetEnablerExtensions().GetOrEmpty(definition);
        }

        public ImmutableDictionary<int, ExtensionInfo> GetExtensions()
        {
            if (_extensions == null)
            {
                var extensions = Db.Query().CommandText("select * from extensions where active = 1")
                                .Execute()
                                .Select(r => new ExtensionInfo(r)).ToDictionary(e => e.id);

                var requiredExtensions = Db.Query().CommandText("select * from extensionprerequire")
                                                .Execute()
                                                .Select(r =>
                                                {
                                                   var id = r.GetValue<int>("requiredextension");
                                                   var level = r.GetValue<int>("requiredlevel");
                                                   return new
                                                   {
                                                       extensionID = r.GetValue<int>("extensionid"),
                                                       requiredExtension = new Extension(id, level)
                                                   };
                                                })
                                                .Where(r => extensions.ContainsKey(r.requiredExtension.id))
                                                .ToLookup(r => r.extensionID, r => r.requiredExtension);

                foreach (var info in extensions.Values)
                {
                    info.RequiredExtensions = requiredExtensions.GetOrEmpty(info.id);
                }

                _extensions = extensions.ToImmutableDictionary();
            }

            return _extensions;
        }

        public Extension[] GetCharacterDefaultExtensions(Character character)
        {
            return GetAllRaceExtensions(character.RaceId)
                .Concat(GetAllSchoolExtensions(character.SchoolId))
                .Concat(GetAllMajorExtensions(character.MajorId))
                .Concat(GetAllSparkExtensions(character.SparkId))
                .Concat(GetAllCorporationExtensions(character.DefaultCorporationEid))
                .GroupBy(e => e.id)
                .Select(grp => new Extension(grp.Key, grp.Sum(g => g.level))).ToArray();
        }

        public ExtensionBonus[] GetRobotComponentExtensionBonus(int robotComponentDefinition)
        {
            if (_robotComponentExtensionBonuses == null)
            {
                var extensions = GetExtensions();
                _robotComponentExtensionBonuses = Database.CreateLookupCache<int, ExtensionBonus>("chassisbonus", "definition", r => new ExtensionBonus(r), r => _entityDefaultReader.Value.Exists(r.GetValue<int>("definition")) && extensions.ContainsKey(r.GetValue<int>("extension")));
            }

            return _robotComponentExtensionBonuses.GetOrEmpty(robotComponentDefinition);
        }

        private IEnumerable<Extension> GetAllRaceExtensions(int raceId)
        {
            return GetCharacterDefaultExtensions("race", "raceId", raceId);
        }

        private IEnumerable<Extension> GetAllSchoolExtensions(int schoolId)
        {
            return GetCharacterDefaultExtensions("school", "schoolId", schoolId);
        }

        private IEnumerable<Extension> GetAllMajorExtensions(int majorId)
        {
            return GetCharacterDefaultExtensions("major", "majorId", majorId);
        }

        private IEnumerable<Extension> GetAllSparkExtensions(int sparkId)
        {
            return GetCharacterDefaultExtensions("spark", "sparkId", sparkId);
        }

        private IEnumerable<Extension> GetAllCorporationExtensions(long corporationEid)
        {
            return GetCharacterDefaultExtensions("corporation", "corporationEID", corporationEid);
        }

        private IEnumerable<Extension> GetCharacterDefaultExtensions(string table, string idName, object id)
        {
            var extensions = GetExtensions();

            return Db.Query().CommandText("select * from cw_" + table + "_extension where " + idName + " = @id")
                .SetParameter("@id", id)
                .Execute()
                .Select(r => new Extension(r.GetValue<int>("extensionid"), r.GetValue<int>("levelincrement")))
                .Where(e => extensions.ContainsKey(e.id));
        }
    }
}