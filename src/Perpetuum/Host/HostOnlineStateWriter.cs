using System;
using Perpetuum.Data;
using Perpetuum.Threading.Process;

namespace Perpetuum.Host
{
    public class HostOnlineStateWriter : Process
    {
        private readonly IHostStateService _hostStateService;

        public HostOnlineStateWriter(IHostStateService hostStateService)
        {
            _hostStateService = hostStateService;
        }

        private static void UpdateHostStateToDb()
        {
            Db.Query().CommandText("update gameglobals set lastonline=@now").SetParameter("@now", DateTime.Now).ExecuteNonQuery();
        }

        public override void Stop()
        {
            UpdateHostStateToDb();
        }

        public override void Update(TimeSpan time)
        {
            if (_hostStateService.State != HostState.Online)
                return;

            UpdateHostStateToDb();
        }
    }
}