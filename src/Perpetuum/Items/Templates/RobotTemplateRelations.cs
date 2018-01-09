using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Items.Templates
{
    public class RobotTemplateRelations : IRobotTemplateRelations
    {
        private readonly IRobotTemplateReader _robotTemplateReader;
        private readonly IEntityDefaultReader _entityDefaultReader;
        private readonly Dictionary<int,IRobotTemplateRelation> _relations = new Dictionary<int, IRobotTemplateRelation>();

        private RobotTemplate _equippedDefault;
        private RobotTemplate _unequippedDefault;

        public RobotTemplateRelations(IRobotTemplateReader robotTemplateReader,IEntityDefaultReader entityDefaultReader)
        {
            _robotTemplateReader = robotTemplateReader;
            _entityDefaultReader = entityDefaultReader;
        }

        public void Init()
        {
            var records = Db.Query().CommandText("select * from robottemplaterelation").Execute();

            foreach (var record in records)
            {
                var relation = CreateRobotTemplateRelationFromRecord(record);
                _relations[relation.EntityDefault.Definition] = relation;
            }
            _equippedDefault = _robotTemplateReader.GetByName("starter_master");
            _unequippedDefault = _robotTemplateReader.GetByName("arkhe_empty");
        }

        private RobotTemplateRelation CreateRobotTemplateRelationFromRecord(IDataRecord record)
        {
            var relation = new RobotTemplateRelation
            {
                EntityDefault = _entityDefaultReader.Get(record.GetValue<int>("definition")),
                Template = _robotTemplateReader.Get(record.GetValue<int>("templateid")),
                RaceID = record.GetValue<int>("raceid"),
                missionLevel = record.GetValue<int?>("missionlevel"),
                missionLevelOverride = record.GetValue<int?>("missionleveloverride")
            };
            return relation;
        }

        public RobotTemplate EquippedDefault => _equippedDefault;
        public RobotTemplate UnequippedDefault => _unequippedDefault;

        public IRobotTemplateRelation Get(int definition)
        {
            return _relations.GetOrDefault(definition);
        }

        public IEnumerable<IRobotTemplateRelation> GetAll()
        {
            return _relations.Values;
        }
    }
}