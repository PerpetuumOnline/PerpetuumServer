using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.Services.Sparks
{
    public class SparkHelper
    {
        public const int SPARK_CHANGE_MINUTES = 60;
        private readonly ISparkRepository _sparkRepository;

        public SparkHelper(ISparkRepository sparkRepository)
        {
            _sparkRepository = sparkRepository;
        }

        public Spark GetSpark(int sparkID)
        {
            return _sparkRepository.Get(sparkID);
        }

        [CanBeNull]
        private UnlockedSpark CreateUnlockedSparkFromRecord(IDataRecord record)
        {
            if (record == null)
                return null;

            var sparkId = record.GetValue<int>("sparkid");
            var active = record.GetValue<bool>("active");
            var activationTime = record.GetValue<DateTime?>("activationtime");

            if (activationTime.Equals(default(DateTime)))
                activationTime = null;

            return new UnlockedSpark(sparkId, active, activationTime);
        }

        [CanBeNull]
        public UnlockedSpark GetUnlockedSpark(Character character, int sparkID)
        {
            var record = Db.Query().CommandText("select * from charactersparks where characterid=@characterID and sparkid = @sparkID")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@sparkID", sparkID)
                .ExecuteSingleRow();
            return CreateUnlockedSparkFromRecord(record);
        }

        public IEnumerable<UnlockedSpark> GetUnlockedSparkData(Character character)
        {
            return Db.Query().CommandText("select sparkid,active,activationtime from charactersparks where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .Execute()
                .Select(CreateUnlockedSparkFromRecord).ToArray();
        }

        public void GetActiveSparkId(Character character, out int sparkId, out DateTime? activationTime)
        {
            var record = Db.Query().CommandText("select top 1 sparkid,activationtime from charactersparks where characterid=@characterID and active=1")
                .SetParameter("@characterID", character.Id)
                .ExecuteSingleRow();

            if (record == null)
            {
                sparkId = 0;
                activationTime = null;
                return;
            }

            sparkId = record.GetValue<int>(0);
            activationTime = record.GetValue<DateTime?>(1);

        }

        public void UnlockSpark(Character character, int sparkId)
        {
            Db.Query().CommandText("insert charactersparks (characterid,sparkid) values (@characterID, @sparkID)")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@sparkID", sparkId)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void SendSparksList(IRequest request)
        {
            var character = request.Session.Character;
            CreateSparksListMessage(character).FromRequest(request).Send();
        }

        public MessageBuilder CreateSparksListMessage(Character character)
        {
            var result = new Dictionary<string, object>
            {
                {"unlockedSparks", GetUnlockedSparkData(character).ToDictionary("s", u => u.ToDictionary())},
                {"sparkInfo", _sparkRepository.GetAll().Where(s => !s.hidden).ToDictionary("s", s => s.ToDictionary())}
            };

            return Message.Builder.SetCommand(Commands.SparkList).WithData(result);
        }

        public void DeactivateSpark(Character character, int sparkId)
        {
            var spark = _sparkRepository.Get(sparkId).ThrowIfNull(ErrorCodes.ItemNotFound);
            SetSparkState(character, sparkId, false).ThrowIfError();
            spark.DeleteRelatedExtensions(character);
        }

        public void ActivateSpark(Character character, int sparkId)
        {
            var spark = _sparkRepository.Get(sparkId);
            if (spark == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            if (spark.defaultSpark)
            {
                if (!IsSparkUnlocked(character, sparkId))
                {
                    //default spark -> unlock it
                    UnlockSpark(character, sparkId);
                }
            }

            SetSparkState(character, sparkId, true).ThrowIfError();
            spark.SetRelatedExtensions(character);
        }

        public bool IsSparkUnlocked(Character character, int sparkId)
        {
            var record = Db.Query().CommandText("select characterid from charactersparks where characterid=@characterID and sparkid=@sparkID")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@sparkID", sparkId)
                .ExecuteSingleRow();

            return record != null;
        }

        private ErrorCodes SetSparkState(Character character, int sparkId, bool state)
        {
            var queryStr = "update charactersparks set active=0,activationtime=NULL where characterid=@characterID and sparkid=@sparkID";
            if (state)
            {
                queryStr = "update charactersparks set active=1,activationtime=getdate() where characterid=@characterID and sparkid=@sparkID";
            }

            var res = Db.Query().CommandText(queryStr)
                .SetParameter("@characterID", character.Id)
                .SetParameter("@sparkID", sparkId)
                .ExecuteNonQuery();

            return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;
        }

        public int ConvertCharacterWizardSparkIdToSpark(int cwSparkId)
        {
            var activeSparkId = 0;

            switch (cwSparkId)
            {
                case 1:
                    activeSparkId = 38;
                    break;

                case 2:
                    activeSparkId = 39;
                    break;

                case 3:
                    activeSparkId = 40;
                    break;

                case 4:
                    activeSparkId = 41;
                    break;

                case 5:
                    activeSparkId = 42;
                    break;

                case 6:
                    activeSparkId = 43;
                    break;

                case 7:
                    activeSparkId = 44;
                    break;

                case 8:
                    activeSparkId = 45;
                    break;

                case 9:
                    activeSparkId = 46;
                    break;

                default:
                    activeSparkId = 38;
                    break;
            }

            return activeSparkId;
        }

        public void ResetSparks(Character character)
        {
            Db.Query().CommandText("delete charactersparks where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteNonQuery();
        }
    }
}