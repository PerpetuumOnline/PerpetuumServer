using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.StatsMapDrawing
{
    public partial class ZoneDrawStatMap
    {
        private void DrawMissionTargetLog(IRequest request)
        {
            var randomCategories = new []
            {
                MissionCategory.Combat, MissionCategory.Transport, MissionCategory.Exploration, MissionCategory.Harvesting,
                MissionCategory.Mining, MissionCategory.Production, MissionCategory.CombatExploration, MissionCategory.ComplexProduction
            };

            var locationsOnZone = _missionDataCache.GetAllLocations.Where(l => l.ZoneConfig.Id == _zone.Id).ToList();
            var allmissions = _missionDataCache.GetAllLiveRandomMissionTemplates.ToList();
            
            var tasks = new List<Task>();
            foreach (var category in randomCategories)
            {
                var randomMissions = allmissions.Where(m => m.missionCategory == category).ToList();
                
                Logger.Info("----------------------");
                Logger.Info(randomMissions.Count + " missions in category " + category);

                var cpus = Environment.ProcessorCount;
                
                foreach (var missionLocation in locationsOnZone)
                {
                    var location = missionLocation;
                    var category1 = category;
                    var oneTask = Task.Factory.StartNew(() => { DrawOneCategory(request,location, category1); }, new CancellationToken(),MissionResolveTester.ResolveTestTaskCreationOptions,TaskScheduler.Default);
                    tasks.Add(oneTask);

                    if (tasks.Count(tsk => !tsk.IsCompleted) < cpus) continue;

                    while (tasks.Count(tsk => !tsk.IsCompleted) > cpus)
                    {
                        Thread.Sleep(50);
                    }
                }
            }

            Logger.Info("waiting for tasks to finish");

            Task.WaitAll(tasks.ToArray());

            Logger.Info("all tasks done.");


            Logger.Info("drawing finished of mission target success log");
            Logger.Info("--------------------------------------------------");
            SendDrawFunctionFinished(request);
            
        }

        internal class MissionTargetSuccessLogEntry
        {
            public DateTime EventTime;
            public Point point;
            public MissionTargetType targetType;
            public Guid guid;
            public long locationEid;
            public MissionCategory category;


            public static MissionTargetSuccessLogEntry FromRecord(IDataRecord record)
            {
                var mtsle = new MissionTargetSuccessLogEntry()
                {
                    EventTime = record.GetValue<DateTime>("eventtime"),
                    point = new Point(record.GetValue<int>("x"), record.GetValue<int>("y")),
                    targetType = (MissionTargetType) record.GetValue<int>("targettype"),
                    guid = record.GetValue<Guid>("guid"),
                    locationEid = record.GetValue<long>("locationeid"),
                    category = (MissionCategory) record.GetValue<int>("missioncategory")
                };

                return mtsle;
            }
        }


        private void DrawOneCategory(IRequest request,MissionLocation missionLocation, MissionCategory category)
        {
            const string query = "SELECT * FROM dbo.missiontargetslog WHERE zoneid=@zoneId and locationeid=@locationEid and missioncategory=@category";

            var entries = Db.Query().CommandText(query)
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@category", (int) category)
                .SetParameter("@locationEid", missionLocation.LocationEid)
                .Execute()
                .Select(MissionTargetSuccessLogEntry.FromRecord).ToArray();

            if (entries.Length == 0)
            {
                Logger.Info("no entry at " + missionLocation + " in category: " + category);
                return;
            }

            var bitmap = _zone.CreatePassableBitmap(_passableColor, _islandColor);
            DrawEntriesOnBitmap(entries,bitmap);

            var ft = _zone.GetUnit(missionLocation.LocationEid);
            var littleText = "locationID:" + missionLocation.id;
            if (ft != null)
                littleText += " " + ft.Name;


            var category1 = category;
            bitmap.WithGraphics(gx => gx.DrawString(category1.ToString(), new Font("Tahoma", 15), new SolidBrush(Color.White), new PointF(20, 40)));
            bitmap.WithGraphics(gx => gx.DrawString(littleText, new Font("Tahoma", 15), new SolidBrush(Color.White), new PointF(20, 60)));

            var idString = $"{missionLocation.id:0000}";

            var fname = "_" + category1 + "_LOC" + idString + "_";
            SendBitmapFinished(request,fname);
            _saveBitmapHelper.SaveBitmap(_zone,bitmap, fname);
        }

        private void DrawEntriesOnBitmap(MissionTargetSuccessLogEntry[] entries, Bitmap background)
        {
            var eventSeries = entries.GroupBy(t => t.guid);

            
            var g = Graphics.FromImage(background);
            var pen = new Pen(new SolidBrush(Color.FromArgb(25, Color.FromArgb(200, 200, 200))), 1.1f);

            var switchBrush = new SolidBrush(Color.FromArgb(25, _switchColor));
            var submitItemBrush = new SolidBrush(Color.FromArgb(25, _kioskColor));
            var itemSupplyBrush = new SolidBrush(Color.FromArgb(25, _itemSupplyColor));
            var findArtifactBrush = new SolidBrush(Color.FromArgb(50, _findArtifactColor));
            var popNpcBrush = new SolidBrush(Color.FromArgb(50, _popNpcColor));
            var lootBrush = new SolidBrush(Color.FromArgb(50, _lootColor));
            var fetchItemBrush = new SolidBrush(Color.FromArgb(25, _fetchItemColor));
            var killBrush = new SolidBrush(Color.FromArgb(30, _killColor));
            var scanMineralBrush = new SolidBrush(Color.FromArgb(50, _scanMineralColor));
            var drillMineralBrush = new SolidBrush(Color.FromArgb(50, _drillMineralColor));
            var harvestBrush = new SolidBrush(Color.FromArgb(50, _harvestColor));

            var circle = 10.0f;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var series in eventSeries)
            {
                var points = series.OrderBy(v => v.EventTime).Select(v => v.point).ToArray();

                g.DrawLines(pen, points);

                var eventsAtStructures = series.Where(s => (
                    s.targetType == MissionTargetType.use_switch ||
                    s.targetType == MissionTargetType.submit_item ||
                    s.targetType == MissionTargetType.use_itemsupply ||
                    s.targetType == MissionTargetType.find_artifact ||
                    s.targetType == MissionTargetType.pop_npc ||
                    s.targetType == MissionTargetType.loot_item ||
                    s.targetType == MissionTargetType.fetch_item ||
                    s.targetType == MissionTargetType.kill_definition ||
                    s.targetType == MissionTargetType.scan_mineral ||
                    s.targetType == MissionTargetType.drill_mineral ||
                    s.targetType == MissionTargetType.harvest_plant
                    ));

                foreach (var logEntry in eventsAtStructures)
                {
                    Brush p;
                    Pen pp;
                    switch (logEntry.targetType)
                    {

                        case MissionTargetType.submit_item:
                            p = submitItemBrush;
                            g.FillEllipse(p, logEntry.point.X - circle / 2.0f, logEntry.point.Y - circle / 2.0f, circle, circle);
                            continue;
                        case MissionTargetType.use_switch:
                            p = switchBrush;
                            g.FillEllipse(p, logEntry.point.X - circle / 2.0f, logEntry.point.Y - circle / 2.0f, circle, circle);
                            continue;
                        case MissionTargetType.use_itemsupply:
                            p = itemSupplyBrush;
                            g.FillEllipse(p, logEntry.point.X - circle / 2.0f, logEntry.point.Y - circle / 2.0f, circle, circle);
                            continue;

                        case MissionTargetType.find_artifact:
                            p = findArtifactBrush;
                            g.FillRectangle(p, logEntry.point.X - circle / 2.0f, logEntry.point.Y - circle / 2.0f, circle, circle);
                            continue;

                        case MissionTargetType.pop_npc:
                            p = popNpcBrush;
                            g.FillRectangle(p, logEntry.point.X - circle / 2.0f, logEntry.point.Y - circle / 2.0f, circle, circle);
                            continue;

                        case MissionTargetType.loot_item:
                            pp = new Pen(lootBrush, 3);
                            const int lootSize = 11;
                            g.DrawRectangle(pp, logEntry.point.X - lootSize / 2.0f, logEntry.point.Y - lootSize / 2.0f, lootSize, lootSize);
                            continue;

                        case MissionTargetType.fetch_item:
                            pp = new Pen(fetchItemBrush, 4);
                            const int fetchSize = 14;
                            g.DrawRectangle(pp, logEntry.point.X - fetchSize / 2.0f, logEntry.point.Y - fetchSize / 2.0f, fetchSize, fetchSize);
                            continue;

                        case MissionTargetType.kill_definition:
                            const int tizenKetto = 12;
                            pp = new Pen(killBrush, 4);
                            g.DrawRectangle(pp, logEntry.point.X - tizenKetto / 2.0f, logEntry.point.Y - tizenKetto / 2.0f, tizenKetto, tizenKetto);
                            continue;


                        case MissionTargetType.scan_mineral:
                            pp = new Pen(scanMineralBrush, 4);
                            g.DrawEllipse(pp, logEntry.point.X - tizenKetto / 2.0f, logEntry.point.Y - tizenKetto / 2.0f, tizenKetto, tizenKetto);
                            continue;

                        case MissionTargetType.drill_mineral:
                            pp = new Pen(drillMineralBrush, 4);
                            g.DrawEllipse(pp, logEntry.point.X - tizenKetto / 2.0f, logEntry.point.Y - tizenKetto / 2.0f, tizenKetto, tizenKetto);
                            continue;

                        case MissionTargetType.harvest_plant:
                            pp = new Pen(harvestBrush, 4);
                            g.DrawEllipse(pp, logEntry.point.X - tizenKetto / 2.0f, logEntry.point.Y - tizenKetto / 2.0f, tizenKetto, tizenKetto);
                            continue;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }

                var startPoint = points[0];
                DrawEllipseOnPoint(_fieldTerminalColor, 7, startPoint.ToPosition(), background);

            }

        }



        private Bitmap DrawAllTargetsOnZone()
        {
            const string query = "SELECT * FROM dbo.missiontargetslog WHERE zoneid=@zoneId";

            var entries = Db.Query().CommandText(query)
                .SetParameter("@zoneId", _zone.Id)
                .Execute()
                .Select(MissionTargetSuccessLogEntry.FromRecord).ToArray();

            var bitmap = _zone.CreateBitmap();
            if (entries.Length == 0)
            {
                Logger.Info("no entry on zone:" + _zone.Id );
                return bitmap;
            }

            
            DrawEntriesOnBitmap(entries, bitmap);

            return bitmap;
        }

    }
}
