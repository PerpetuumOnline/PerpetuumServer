using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Threading.Process;
using System;

namespace Perpetuum.Services
{
    public class ArtifactRefresher : Process
    {
        public override void Update(TimeSpan time)
        {
            DoRefresh();
        }

        public void DoRefresh()
        {
            Logger.Info("Artifact Refresh");
            Db.Query().CommandText("EXEC [opp].[artifactRefresh];").ExecuteNonQuery();
        }
    }
}
