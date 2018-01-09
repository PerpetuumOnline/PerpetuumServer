using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.GenXY;

namespace Perpetuum.Items.Templates
{
    public class RobotTemplateRepository : IRobotTemplateRepository
    {
        private readonly IRobotTemplateReader _templateReader;

        public RobotTemplateRepository(IRobotTemplateReader templateReader)
        {
            _templateReader = templateReader;
        }

        public void Insert(RobotTemplate template)
        {
            var descriptionString = GenxyConverter.Serialize(template.ToDictionary());

            var id = Db.Query().CommandText("insert robottemplates (name,description) values (@name,@description); select cast(scope_identity() as integer)")
                .SetParameter("@name",template.Name)
                .SetParameter("@description", descriptionString)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            template.ID = id;
        }
        
        public void Update(RobotTemplate template)
        {
            var descriptionString = GenxyConverter.Serialize(template.ToDictionary());

            Db.Query().CommandText("update robottemplates set name=@name,description=@description where id=@id")
                .SetParameter("@id",template.ID)
                .SetParameter("@name",template.Name)
                .SetParameter("@description", descriptionString)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public void DeleteByID(int templateID)
        {
            Db.Query().CommandText("delete robottemplates where id=@id")
                .SetParameter("@id", templateID)
                .ExecuteNonQuery().ThrowIfNotEqual(1, ErrorCodes.SQLDeleteError);
        }

        public void Delete(RobotTemplate item)
        {
            DeleteByID(item.ID);
        }

        public RobotTemplate Get(int id)
        {
            return _templateReader.Get(id);
        }

        public IEnumerable<RobotTemplate> GetAll()
        {
            return _templateReader.GetAll();
        }
    }
}