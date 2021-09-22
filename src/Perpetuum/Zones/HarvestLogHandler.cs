using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Timers;

namespace Perpetuum.Zones
{
    public class HarvestLogHandler
    {
        private readonly IZone _zone;
        private readonly ConcurrentQueue<HarvestLogEntry> _HarvestLogs = new ConcurrentQueue<HarvestLogEntry>();

        public delegate HarvestLogHandler Factory(IZone zone);

        public HarvestLogHandler(IZone zone)
        {
            _zone = zone;
        }

        public void EnqueueHarvestLog(int drilledMineralDefinition, int drilledQuantity)
        {
            var entry = new HarvestLogEntry
            {
                definition = drilledMineralDefinition,
                quantity = drilledQuantity,
                eventTime = DateTime.Today
            };

            _HarvestLogs.Enqueue(entry);
        }

        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(10));

        public void Update(TimeSpan time)
        {
            _timer.Update(time).IsPassed(() => Task.Run(() => WriteHarvestLogToSql()));
        }

        private void WriteHarvestLogToSql()
        {
            if (_HarvestLogs.Count == 0)
                return;

            Logger.Info("flushing Harvestlog. " + _HarvestLogs.Count + " long entries. ");

            var finalList = new List<HarvestLogEntry>();

            var workList = _HarvestLogs.TakeAll().ToList();

            while (workList.Count > 0)
            {
                var firstItem = workList[0];
                workList.RemoveAt(0);

                if (workList.Count > 0)
                {
                    firstItem.quantity += workList.Where(e => e.definition == firstItem.definition && e.eventTime == firstItem.eventTime).Sum(l => l.quantity);
                    workList.RemoveAll(e => e.definition == firstItem.definition && e.eventTime == firstItem.eventTime);
                }

                finalList.Add(firstItem);
            }

            var counter = 0;
            if (finalList.Count <= 0)
                return;

            foreach (var me in finalList)
            {
                Db.Query().CommandText("writeharvestlog")
                       .SetParameter("@zoneId", _zone.Id)
                       .SetParameter("@definition", me.definition)
                       .SetParameter("@quantity", me.quantity)
                       .SetParameter("@eventtime", me.eventTime)
                       .ExecuteNonQuery();

                counter++;
            }

            Logger.Info("Harvestlog fushed " + counter + " actual entries.");
        }

        private struct HarvestLogEntry
        {
            public int definition;
            public int quantity;
            public DateTime eventTime;
        }
    }
}