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
    public class MiningLogHandler
    {
        private readonly IZone _zone;
        private readonly ConcurrentQueue<MiningLogEntry> _miningLogs = new ConcurrentQueue<MiningLogEntry>();

        public delegate MiningLogHandler Factory(IZone zone);

        public MiningLogHandler(IZone zone)
        {
            _zone = zone;
        }

        public void EnqueueMiningLog(int drilledMineralDefinition, int drilledQuantity)
        {
            var entry = new MiningLogEntry
            {
                definition = drilledMineralDefinition,
                quantity = drilledQuantity,
                eventTime = DateTime.Today
            };

            _miningLogs.Enqueue(entry);
        }

        private readonly IntervalTimer _timer = new IntervalTimer(TimeSpan.FromSeconds(10));

        public void Update(TimeSpan time)
        {
            _timer.Update(time).IsPassed(() => Task.Run(() => WriteMiningLogToSql()));
        }

        private void WriteMiningLogToSql()
        {
            if (_miningLogs.Count == 0) 
                return;

            Logger.Info("flushing mininglog. " + _miningLogs.Count + " long entries. ");

            var finalList = new List<MiningLogEntry>();

            var workList = _miningLogs.TakeAll().ToList();

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
                Db.Query().CommandText("writemininglog")
                       .SetParameter("@zoneId",_zone.Id)
                       .SetParameter("@definition", me.definition)
                       .SetParameter("@quantity", me.quantity)
                       .SetParameter("@eventtime", me.eventTime)
                       .ExecuteNonQuery();

                counter++;
            }

            Logger.Info("mininglog fushed " + counter + " actual entries.");
        }

        private struct MiningLogEntry
        {
            public int definition;
            public int quantity;
            public DateTime eventTime;
        }
    }
}