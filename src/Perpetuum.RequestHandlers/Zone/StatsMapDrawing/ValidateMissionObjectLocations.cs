using System.Drawing;
using System.Linq;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.StatsMapDrawing
{
    public partial class ZoneDrawStatMap
    {


        public Bitmap ValidateMissionObjectLocations()
        {
            var b = _zone.CreatePassableBitmap(_passableColor);
            var g = Graphics.FromImage(b);
            var circle = 10f;
            
            var randomPointTargets = _missionDataCache.GetAllMissionTargets.Where(t => t.ZoneId == _zone.Id && t.Type == MissionTargetType.rnd_point).ToList();
              
            var greebrush = new SolidBrush(Color.LawnGreen);
            var redBrush = new SolidBrush(Color.OrangeRed);
            var yellowBrush = new SolidBrush(Color.Yellow);
            var redPen = new Pen(Color.Red, 4);

            foreach (var randomPointTarget in randomPointTargets)
            {
                var p = randomPointTarget.targetPosition.ToPoint().ToPosition();

                if (CheckConditionsAroundPosition(p, randomPointBlockRadius, randomPointIslandRadius, true))
                {
                    g.FillEllipse(greebrush,(float)( randomPointTarget.targetPosition.X - circle ), (float)( randomPointTarget.targetPosition.Y - circle ), circle*2, circle*2);
                }
                else
                {
                    g.FillEllipse(redBrush, (float)(randomPointTarget.targetPosition.X - circle ), (float)(randomPointTarget.targetPosition.Y - circle ), circle*2, circle*2);
                }
            }

            var strucureUnits = _zone.Units.Where(u => u is MissionStructure).Cast<MissionStructure>().ToList();


            foreach (var structureUnit in strucureUnits)
            {
                var strucureTarget = _missionDataCache.GetTargetByStructureUnit(structureUnit);

                if (strucureTarget == null)
                {
                    Logger.Error("no target was found for structure:" + structureUnit.Eid + " " + structureUnit.TargetType);
                    g.FillEllipse(yellowBrush, structureUnit.CurrentPosition.intX - circle, structureUnit.CurrentPosition.intY - circle, circle*2, circle*2);
                    continue;

                }

                strucureTarget.UpdatePositionById(structureUnit.CurrentPosition);

            }


            var locationUnits = _zone.Units.Where(u => u is DockingBase || u is FieldTerminal).ToList();

            foreach (var locationUnit in locationUnits)
            {
                var location = _missionDataCache.GetLocationByEid(locationUnit.Eid);

                if (location == null)
                {
                    g.DrawEllipse(redPen, locationUnit.CurrentPosition.intX - circle, locationUnit.CurrentPosition.intY - circle, circle * 2, circle * 2);
                    Logger.Error("no location was found for " + locationUnit);
                    continue;
                }

                location.UpdatePositionById(locationUnit.CurrentPosition);

            }

            return b;
        }
    }
}
