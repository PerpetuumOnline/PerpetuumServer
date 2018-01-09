using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.MissionTargets;

namespace Perpetuum.Services.MissionEngine
{
    public class MissionResolveTester
    {
        public static bool isTestMode;
        public static bool skipLog;

        private List<Position> _terminalsOnZones = new List<Position>();
        
        private readonly MissionProcessor _missionProcessor;
        private readonly int _zoneId;

        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public MissionResolveTester(MissionProcessor missionProcessor, int zoneId)
        {
            _missionProcessor = missionProcessor;
            _zoneId = zoneId;
        }

        public const TaskCreationOptions ResolveTestTaskCreationOptions = (TaskCreationOptions)((int)TaskContinuationOptions.LongRunning | (int)TaskCreationOptions.PreferFairness | (int)TaskCreationOptions.DenyChildAttach);
        public void RunTestParallel(Character testCharacter, int missionLevel, int maxAttemts, bool writeResult, bool singleLocation)
        {
            LoadTerminalPositions();
            
            isTestMode = true;

            if (writeResult)
            {
                skipLog = true;
            }

            var randomTemplates = _missionDataCache.GetAllLiveRandomMissionTemplates.ToList();
            var locations = _missionDataCache.GetAllLocations.Where(l => l.ZoneConfig.Id == _zoneId).ToList();

            var templateCount = 0;
            if (singleLocation)
            {
                var oneLocation = locations.FirstOrDefault();

                foreach (var mission in randomTemplates)
                {
                    templateCount++;
                    Logger.Info("---------------------------------------------" + templateCount + "/" + randomTemplates.Count);
                    Logger.Info("resolving template " + mission);

                    var tester = new OneLocationTest(_missionProcessor, _terminalsOnZones);
                    tester.TestOne(oneLocation, mission, testCharacter, missionLevel, maxAttemts, writeResult);
                }

                return;
            }

            var cpus = Environment.ProcessorCount*8;
            var tasks = new List<Task>();
            foreach (var mission in randomTemplates)
            {
                templateCount++;
                Logger.Info("---------------------------------------------" + templateCount + "/" + randomTemplates.Count);
                Logger.Info("resolving template " + mission);

                var mission1 = mission;
                
                foreach (var location in locations)
                {
                    var l = location;

                    var tester = new OneLocationTest(_missionProcessor, _terminalsOnZones);

                    var task = Task.Factory.StartNew(() => { tester.TestOne(l, mission1, testCharacter, missionLevel, maxAttemts, writeResult); }, new CancellationToken(), ResolveTestTaskCreationOptions, TaskScheduler.Default);
                    tasks.Add(task);

                    if (tasks.Count(tsk=>!tsk.IsCompleted) < cpus) continue;

                    while (tasks.Count(tsk=>!tsk.IsCompleted) > cpus)
                    {
                        Thread.Sleep(50);
                    }
                    
                }

                Logger.Info("locations done. tasks running:" + tasks.Count(t => !t.IsCompleted));
              
            }

            
            Logger.Info("all tests done. waiting for results. tasks running:" + tasks.Count(t => !t.IsCompleted));

            Task.WaitAll(tasks.ToArray());

            Logger.Info("all tasks done.");

            if (writeResult)
            {
                FlushLogInfos();
            }


            Logger.Info("---------------------");
            Logger.Info("Resolve test done");
        }

       



        private void LoadTerminalPositions()
        {
            const string qs = @"SELECT x,y FROM dbo.zoneentities WHERE 
eid IN 
(SELECT eid FROM dbo.entities WHERE definition IN (SELECT definition FROM dbo.getDefinitionByCFString('cf_public_docking_base')))
AND
zoneID=@zoneId";


            _terminalsOnZones =
                Db.Query().CommandText(qs).SetParameter("@zoneId", _zoneId).Execute().Select(r => new Position(r.GetValue<double>("x"), r.GetValue<double>("y"))).ToList();
        }

        private static readonly ConcurrentQueue<SuccessLogInfo> LogInfos = new ConcurrentQueue<SuccessLogInfo>();

        public static void EnqueSuccesLogInfo(SuccessLogInfo si)
        {
            LogInfos.Enqueue(si);
        }

        
        private void FlushLogInfos()
        {
            var current = 0;
            var nofLogInfos = LogInfos.Count;
            Logger.Info(nofLogInfos + " log infos generated");
            var count = 0;

            var insertList = new List<string>();
            SuccessLogInfo si;
            while (LogInfos.TryDequeue(out si))
            {
                si.AddToInsertList(insertList);
                current++;

                count++;
                if (count > 55000)
                {
                    Logger.Info("");
                    Logger.Info((100.0 * current/nofLogInfos) + "%" );

                    FlushInsertList(insertList);
                    count = 0;
                }

            }

            FlushInsertList(insertList);

            Task.WaitAll(_insertTasks.ToArray());

            Logger.Info(current + " missiontargetlog written.");
        }

        private void FlushInsertList(List<string> insertList)
        {
            Logger.Info("flushing " + insertList.Count);

            var count = 0;

            var lastIndex = insertList.Count - 1;
            var sb = new StringBuilder(insertList.Count * 128);

            foreach (var line in insertList)
            {
                sb.Append(line);

                if (count != lastIndex)
                    sb.Append(";");

                count++;
            }

            InsertChunk(sb.ToString(), insertList.Count);

            

            insertList.Clear();

        }


        private readonly List<Task> _insertTasks = new List<Task>(); 
        private void InsertChunk(string chunk, int checkAmount)
        {
            
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    var result =
                        Db.Query().CommandText(chunk).ExecuteNonQuery();
                    Logger.Info("******** inserted rows:" + result + " in the list:" + checkAmount);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }

            },ResolveTestTaskCreationOptions);
            _insertTasks.Add(task);
            Logger.Info(_insertTasks.Count(t => !t.IsCompleted) + " unfinished");

        }

    }
}