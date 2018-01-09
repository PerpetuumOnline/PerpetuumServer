using System;
using Perpetuum.Data;

namespace Perpetuum.Host
{
    public static class HostInfo
    {
        public static DateTime GetLastOnline()
        {
            return Db.Query().CommandText("select top(1) lastonline from gameglobals").ExecuteScalar<DateTime>();
        }
    }
}