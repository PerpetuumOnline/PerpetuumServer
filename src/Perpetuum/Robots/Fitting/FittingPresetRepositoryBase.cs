using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Robots.Fitting
{
    public abstract class FittingPresetRepositoryBase : IFittingPresetRepository
    {

        public abstract FittingPreset Get(int id);
        public abstract IEnumerable<FittingPreset> GetAll();

        public virtual void Insert(FittingPreset preset)
        {
            preset.Id = Db.Query().CommandText("insert into robotfittingpresets (ownerEid,preset) values (@ownerEid,@preset);select cast(scope_identity() as int)")
                .SetParameter("@ownerEid", preset.Owner)
                .SetParameter("@preset", (string)preset.ToGenxyString())
                .ExecuteScalar<int>().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }

        public void Update(FittingPreset preset)
        {
            Db.Query().CommandText("update robotfittingpresets set preset = @preset where id = @id")
                .SetParameter("@id", preset.Id)
                .SetParameter("@preset", preset.ToGenxyString())
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public void Delete(FittingPreset preset)
        {
            DeleteById(preset.Id);
        }

        public void DeleteById(int id)
        {
            Db.Query().CommandText("delete from robotfittingpresets where id = @id").SetParameter("@id", id).ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        public FittingPreset Get(int id,long ownerEid)
        {
            var record = Db.Query().CommandText("select * from robotfittingpresets where id = @id and ownerEid = @ownerEid")
                .SetParameter("@id", id)
                .SetParameter("@ownerEid",ownerEid)
                .ExecuteSingleRow();

            if (record == null)
                return null;

            return CreateRobotFittingPresetFromRecord(record);
        }

        private static FittingPreset CreateRobotFittingPresetFromRecord(IDataRecord record)
        {
            var presetString = record.GetValue<string>("preset");
            var preset = FittingPreset.CreateFrom(presetString);
            preset.Id = record.GetValue<int>("id");
            preset.Owner = record.GetValue<long>("ownerEid");
            return preset;
        }

        protected static IEnumerable<FittingPreset> GetAll(long ownerEid)
        {
            return Db.Query().CommandText("select * from robotfittingpresets where ownerEid = @ownerEid")
                .SetParameter("@ownerEid", ownerEid)
                .Execute()
                .Select(CreateRobotFittingPresetFromRecord).ToArray();
        }
    }
}