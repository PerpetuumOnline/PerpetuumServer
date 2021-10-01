using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;

namespace Perpetuum.Zones.Terrains.Materials.Plants
{
    public interface IPlantHandler : IProcess
    {
        TimeSpan RenewSpeed { set; }
        Area WorkArea { set; }
        PlantScannerMode ScannerMode { set; }
        IDictionary<string,object> GetInfoDictionary();
    }

    public class PlantHandler : IPlantHandler
    {
        public delegate IPlantHandler Factory(IZone zone);

        private const int AREA_SIZE = 32;
        // The number of 32x32 cubes across the zone max width of 2048x2048
        private const int TIME_SCALING_BASE = 64;

        private readonly IZone _zone;
        private readonly TimeSpan _natureSleepAmount = TimeSpan.FromSeconds(7);
        private IntervalTimer _plantsTimer = new IntervalTimer(TimeSpan.FromSeconds(7));
        private PlantScannerMode _scannerMode = PlantScannerMode.Paused;

        private bool _zoneFinished;
        private int _currentX;
        private int _currentY;
        private int _areaDoneX;
        private int _areaDoneY;
        private readonly int _areaAmount;
        private bool _stopSignal;

        public Area WorkArea { private get; set; }

        public PlantHandler(IZone zone)
        {
            _zone = zone;
            _areaAmount = zone.Size.Width / AREA_SIZE; //the amount of areas
            // We want to scale the time it takes to repopulate a zone
            AddTimerScaling(_areaAmount);
            WorkArea = zone.Size.ToArea();

            ScannerMode = PlantScannerMode.Scanner;
            ResetState();
        }

        private int _isInProcess;

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Update(TimeSpan time)
        {
            _plantsTimer.Update(time);

            if (!_plantsTimer.Passed) 
                return;

            _plantsTimer.Reset();

            if ( Interlocked.CompareExchange(ref _isInProcess,1,0) == 1)
                return;

            Task.Run(() => ProcessPlants()).ContinueWith(t =>
            {
                _isInProcess = 0;
                _stopSignal = false;
            });
        }

        // https://github.com/OpenPerpetuum/PerpetuumServer/issues/304
        private void AddTimerScaling(int zoneWidth)
        {
            _plantsTimer = new IntervalTimer(TimeSpan.FromSeconds(TIME_SCALING_BASE / zoneWidth));
        }

        private void ProcessPlants()
        {
            switch (ScannerMode)
            {
                case PlantScannerMode.Paused: //nothing to do
                    break;
                case PlantScannerMode.Scanner:
                    ScanZone();
                    break;
                case PlantScannerMode.Populate:
                case PlantScannerMode.Renew:
                    PopulateZone();
                    break;
                case PlantScannerMode.FullMapScan:
                    FullMapScan();
                    break;
                case PlantScannerMode.Correct:
                    CorrectZone();
                    break;
            }
        }

        private void PopulateZone()
        {
            if (_zoneFinished)
            {
                ScannerMode = PlantScannerMode.Paused;
            }

            ScanZone();
        }

        private void FullMapScan()
        {
            //do full map in one round            
            while (!_zoneFinished)
            {
                if (_stopSignal) return;
                ScanZone();
            }

            Logger.Info("fullmapscan on zone done:" + _zone.Id);
            _zoneFinished = false; //ez menjen erobol
        }

        private void ScanZone()
        {
            using (new TerrainUpdateMonitor(_zone))
            {
                if (_stopSignal)
                {
                    Logger.Info("Planthandler STOP SIGNAL received. zone:"+ _zone.Id);
                    return;
                }

                var area = GetNewArea();
#if (DEBUG)
                if (ScannerMode == PlantScannerMode.Scanner && area.X1 == 0 && area.Y1 == 0)
                {
                    Logger.Info("plant scan starts on zone:" + _zone.Id);
                }
#endif
                _zone.UpdateNatureCube(area, cube => cube.ProcessAll());
            }
        }

        private void CorrectZone()
        {
            using (new TerrainUpdateMonitor(_zone))
            {
                if (_stopSignal)
                {
                    Logger.Info("Planthandler STOP SIGNAL received. zone:" + _zone.Id);
                    return;
                }

                //do full map in one round            
                while (!_zoneFinished)
                {
                    var area = GetNewArea();
                    _zone.UpdateNatureCube(area, cube => cube.CorrectOnly());
                }
            }

            Logger.Info("plants correct done on zone: " + _zone.Id);
            ScannerMode = PlantScannerMode.Paused;
        }

        private Area GetNewArea()
        {
            Area area;
            do
            {
                area = GetNextArea();
            } while (!WorkArea.Contains(area));

            return area;
        }

        private Area GetNextArea()
        {
            _currentX = _areaDoneX * AREA_SIZE;
            _currentY = _areaDoneY * AREA_SIZE;

            if (++_areaDoneX >= _areaAmount)
            {
                _areaDoneX = 0;

                if (++_areaDoneY >= _areaAmount)
                {
                    _areaDoneY = 0;
                    _zoneFinished = true;
                }
            }

            return Area.FromRectangle(_currentX, _currentY, AREA_SIZE, AREA_SIZE);
        }

        private void SetSpeedLow()
        {
            RenewSpeed = _natureSleepAmount;
        }

        private void SetSpeedHigh()
        {
            RenewSpeed = TimeSpan.FromMilliseconds(1);
        }

        public TimeSpan RenewSpeed
        {
            set { _plantsTimer = new IntervalTimer(value); }
        }

        private void ResetState()
        {
            _stopSignal = true;
            _areaDoneX = 0;
            _areaDoneY = 0;
            _currentX = 0;
            _currentY = 0;
        }

        public PlantScannerMode ScannerMode
        {
            set
            {
                if ( _scannerMode == value )
                    return;

                var preValue = _scannerMode;
                _scannerMode = value;

                Logger.Info("Plant scanner mode is set " + preValue + " -> " + _scannerMode +" on " + _zone.Id);

                ResetState();

                switch (_scannerMode)
                {
                    case PlantScannerMode.Paused:
                        SetSpeedLow();
                        break;

                    case PlantScannerMode.Populate:
                        SetSpeedHigh();
                        break;

                    case PlantScannerMode.Renew:
                        SetSpeedHigh();
                        break;

                    case PlantScannerMode.Scanner:
                        SetSpeedLow();
                        break;

                    case PlantScannerMode.FullMapScan:
                        SetSpeedHigh();
                        break;

                    case PlantScannerMode.Correct:
                        SetSpeedHigh();
                        break;

                    case PlantScannerMode.Natural:
                        SetSpeedHigh();
                        break;
                }

                _zoneFinished = false;
            }

            get { return _scannerMode; }

        }

        public IDictionary<string, object> GetInfoDictionary()
        {
            var result = new Dictionary<string, object>
            {
                {k.mode,_scannerMode.ToString()},
                {k.area, WorkArea},
                {k.speed,(int)_plantsTimer.Interval.TotalMilliseconds},
                {k.position, new Position(_currentX, _currentY)},
                {k.size, AREA_SIZE},
            };
            return result;
        }
    }
}