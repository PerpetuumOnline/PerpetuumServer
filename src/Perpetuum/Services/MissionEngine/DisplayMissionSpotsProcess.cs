using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Threading.Process;
using Perpetuum.Zones;

namespace Perpetuum.Services.MissionEngine
{
    public class DisplayMissionSpotsProcess : Process
    {
        public bool LiveMode { get; set; }

        private List<MissionSpot> _spots;

        public delegate DisplayMissionSpotsProcess Factory(IZone zone);

        public DisplayMissionSpotsProcess(IZone zone)
        {
            Zone = zone;
        }

        public IZone Zone { get; }

        public override void Start()
        {
            _spots = CollectMissionSpots();
            base.Start();
        }

        public override void Update(TimeSpan time)
        {
            DoBeams();
        }

        private List<MissionSpot> CollectMissionSpots()
        {
            if (LiveMode)
            {
                //display the data from live units and targets
                //here we dont really need field terminals
                var spotInfos = MissionSpot.GetMissionSpotsFromUnitsOnZone(Zone)
                                           .Where(s => s.type == MissionSpotType.mswitch || s.type == MissionSpotType.kiosk || s.type == MissionSpotType.itemsupply).ToList();
                var randomPointsInfos = MissionSpot.GetRandomPointSpotsFromTargets(Zone.Configuration);
                spotInfos.AddRange(randomPointsInfos);
                return spotInfos;
            }

            //display the generated data
            var spots = MissionSpot.LoadByZoneId(Zone.Id);
            return spots;
        }

        private void DoBeams()
        {
            foreach (var missionSpot in _spots)
            {
                var pos = missionSpot.position.Center;

                pos = Zone.FixZ(pos);

                switch (missionSpot.type)
                {
                    case MissionSpotType.fieldterminal:

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        break;


                    case MissionSpotType.mswitch:
                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        break;


                    case MissionSpotType.kiosk:


                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);


                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        break;
                    case MissionSpotType.itemsupply:


                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);


                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);


                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);


                        break;
                    case MissionSpotType.randompoint:

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.red_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.green_20sec, pos);

                        pos = new Position(pos.X, pos.Y, pos.Z + 2);
                        Zone.CreateDebugBeam(BeamType.blue_20sec, pos);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}