using System;
using Perpetuum.Data;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.ExtensionService
{
    public class CleanUpPayingCustomersService : Process
    {
        public override void Update(TimeSpan time)
        {
            Db.Query().CommandText("cleanUpPayingCustomers").ExecuteNonQuery();
        }
    }
}