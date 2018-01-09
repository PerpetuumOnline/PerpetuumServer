using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.GenXY;

namespace Perpetuum.Items.Templates
{
    public class RobotTemplateReader : IRobotTemplateReader
    {
        public IEnumerable<RobotTemplate> GetAll()
        {
            return Db.Query().CommandText("select * from robottemplates").Execute().Select(CreateRobotTemplateFromRecord).ToList();
        }

        [CanBeNull]
        public RobotTemplate Get(int templateID)
        {
            var record = Db.Query().CommandText("select * from robottemplates where id = @templateID").SetParameter("@templateID", templateID).ExecuteSingleRow();
            if (record == null)
                return null;

            return CreateRobotTemplateFromRecord(record);
        }

        private static RobotTemplate CreateRobotTemplateFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var name = record.GetValue<string>("name");
            var description = record.GetValue<string>("description");
            var dictionary = GenxyConverter.Deserialize(description);
            var template = RobotTemplate.CreateFromDictionary(name, dictionary);
            if (template == null)
            {
                return null;
            }

            template.ID = id;
            return template;
        }
    }
}