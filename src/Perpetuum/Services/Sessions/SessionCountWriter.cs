using System;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.Sessions
{
    public class SessionCountWriter : IProcess
    {
        private readonly ISessionManager _sessionManager;

        public SessionCountWriter(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            WriteSessionCountToDb();
        }

        private void WriteSessionCountToDb()
        {
            var userCount = _sessionManager.Sessions.Count(c => c.IsAuthenticated);
            Db.Query().CommandText("insert usercount (usercount) values (@userCount)").SetParameter("@userCount", userCount).ExecuteNonQuery();
        }
    }
}