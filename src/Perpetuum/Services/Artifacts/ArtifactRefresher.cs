using Perpetuum.Data;
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
            Console.WriteLine("Refreshing Artifacts");
            Db.Query().CommandText("EXEC [opp].[artifactRefresh];").ExecuteNonQuery();
        }
    }
}
