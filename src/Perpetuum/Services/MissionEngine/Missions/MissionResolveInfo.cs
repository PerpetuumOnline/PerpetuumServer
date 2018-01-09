using System.Data;
using Perpetuum.Data;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.Services.MissionEngine.Missions
{
    public class MissionResolveInfo
    {
        public int missionId;
        public int locationId;
        public int attempts;
        private int _success;

        public bool IsSafeToResolve
        {
            get
            {
                if (_success == 0 || attempts == 0) return false;
                
                return _success / (double) attempts > 0.4;
            }
        }

        public static MissionResolveInfo FromRecord(IDataRecord record)
        {
            var info = new MissionResolveInfo
            {
                missionId = record.GetValue<int>("missionid"), 
                locationId = record.GetValue<int>("locationid"), 
                attempts = record.GetValue<int>("attempts"), 
                _success = record.GetValue<int>("success")
            };

            return info;
        }

        public static void InsertToDb(Mission mission, MissionLocation location, int attempts, int success, int uniquecases, int rewardAverage)
        {
            var res=
            Db.Query().CommandText("INSERT dbo.missiontolocation ( missionid, locationid, attempts, success, uniquecases,rewardaverage) VALUES  ( @missionid,@locationid,@attempts,@success,@uniquecases,@rewardAverage)")
                .SetParameter("@missionid", mission.id)
                .SetParameter("@locationid", location.id)
                .SetParameter("@attempts", attempts)
                .SetParameter("@success", success)
                .SetParameter("@uniquecases", uniquecases)
                .SetParameter("@rewardAverage", rewardAverage)
                .ExecuteNonQuery();

            (res == 1).ThrowIfFalse(ErrorCodes.SQLInsertError);
        }
    }
}
