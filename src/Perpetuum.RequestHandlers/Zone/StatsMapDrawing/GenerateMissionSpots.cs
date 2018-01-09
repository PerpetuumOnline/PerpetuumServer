using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.RequestHandlers.Zone.StatsMapDrawing
{
    public partial class ZoneDrawStatMap
    {
        private class AccuracyInfo
        {
            public int blockRadius;
            public int islandRadius;
            public int initialBorder;
            public int borderIncrease;
        }


        private const int fieldTerminalBlockRadius = 5;
        private const int structureBlockingRadius = 5;
        private const int randomPointBlockRadius = 5;

        private const int fieldTerminalIslandRadius = 25;
        private const int structureIslandRadius = 20;
        private const int randomPointIslandRadius = 15;

        //---------- config

        private const int fieldTerminalToTeleports = 60;
        private const int structureToTeleports = 50;
        private const int randomPointToTeleports = 40;

        private const int fieldTerminalToSaps = 60;
        private const int structureToSaps = 50;
        private const int randomPointToSaps = 40;

        private const int fieldTerminalToFieldTerminal = 240;

        //-----------------------------------------
        // findRadius minimum annyi mint a legnagyobb ezek kozul 

        private const int structureToStructureOtherType = 3;
        private const int structureToStructureMyType = 140;
        private const int structureToFieldTerminal = 40;

        private const int randomPointToFieldTerminal = 50;
        private const int randomPointToStructure = 20;
        private const int randomPointToRandomPoint = 30;

        private const int fieldTerminalToTerminals = 60;

        private readonly Dictionary<MissionSpotType, int> fieldTerminalDistanceInfos = new Dictionary<MissionSpotType, int>
        {
            {MissionSpotType.fieldterminal, fieldTerminalToFieldTerminal},
            {MissionSpotType.terminal, fieldTerminalToTerminals},
            {MissionSpotType.teleport, fieldTerminalToTeleports},
            {MissionSpotType.sap, fieldTerminalToSaps}
        };

        private readonly Dictionary<MissionSpotType, int> switchDistanceInfos = new Dictionary<MissionSpotType, int>
        {
            {MissionSpotType.fieldterminal, structureToFieldTerminal},
            {MissionSpotType.mswitch, structureToStructureMyType},
            {MissionSpotType.kiosk, structureToStructureOtherType},
            {MissionSpotType.itemsupply, structureToStructureOtherType},
            {MissionSpotType.terminal, structureToTerminals},
            {MissionSpotType.teleport, structureToTeleports},
            {MissionSpotType.sap, structureToSaps}
        };

        private readonly Dictionary<MissionSpotType, int> kioskDistanceInfos = new Dictionary<MissionSpotType, int>
        {
            {MissionSpotType.fieldterminal, structureToFieldTerminal},
            {MissionSpotType.mswitch, structureToStructureOtherType},
            {MissionSpotType.kiosk, structureToStructureMyType},
            {MissionSpotType.itemsupply, structureToStructureOtherType},
            {MissionSpotType.terminal, structureToTerminals},
            {MissionSpotType.teleport, structureToTeleports},
            {MissionSpotType.sap, structureToSaps}
        };

        private readonly Dictionary<MissionSpotType, int> itemSupplyDistanceInfos = new Dictionary<MissionSpotType, int>
        {
            {MissionSpotType.fieldterminal, structureToFieldTerminal},
            {MissionSpotType.mswitch, structureToStructureOtherType},
            {MissionSpotType.kiosk, structureToStructureOtherType},
            {MissionSpotType.itemsupply, structureToStructureMyType},
            {MissionSpotType.terminal, structureToTerminals},
            {MissionSpotType.teleport, structureToTeleports},
            {MissionSpotType.sap, structureToSaps}
        };

        private readonly Dictionary<MissionSpotType, int> randomPointDistanceInfos = new Dictionary<MissionSpotType, int>
        {
            {MissionSpotType.fieldterminal, randomPointToFieldTerminal},
            {MissionSpotType.mswitch, randomPointToStructure},
            {MissionSpotType.kiosk, randomPointToStructure},
            {MissionSpotType.itemsupply, randomPointToStructure},
            {MissionSpotType.randompoint, randomPointToRandomPoint},
            {MissionSpotType.terminal, randomPointToTerminals},
            {MissionSpotType.teleport, randomPointToTeleports},
            {MissionSpotType.sap, randomPointToSaps}
        };

        private readonly AccuracyInfo fieldTerminalAccuracy = new AccuracyInfo()
        {
            blockRadius = fieldTerminalBlockRadius,
            islandRadius = fieldTerminalIslandRadius,
            borderIncrease = fieldTerminalToFieldTerminal / 2,
            initialBorder = fieldTerminalToFieldTerminal * 2,
        };

        private readonly AccuracyInfo structureAccuracy = new AccuracyInfo()
        {
            blockRadius = structureBlockingRadius,
            islandRadius = structureIslandRadius,
            borderIncrease = structureToStructureMyType / 2,
            initialBorder = structureToStructureMyType * 2,
        };

        private readonly AccuracyInfo randomPointAccuracy = new AccuracyInfo()
        {
            blockRadius = randomPointBlockRadius,
            islandRadius = randomPointIslandRadius,
            borderIncrease = randomPointToRandomPoint / 5,
            initialBorder = (int) (randomPointToRandomPoint * 1.5),
        };




        //------------------------------------------
        // ezen mindenkeppen alacsonyabbak kell h legyenek mint a find radius,
        // kulonben a terminalbol indulok mission pl nem fogja megtalalni a targetet
        private const int structureToTerminals = 50;
        private const int randomPointToTerminals = 70;

        private Bitmap GenerateMissionSpots(IRequest request)
        {
            //-------- kick brute force fill in

            const int fieldTerminalTargetAmount = 2500;
            const int switchTargetAmount = 2500;
            const int kioskTargetAmount = 2500;
            const int itemSupplyTargetAmount = 2500;
            const int randomPointTargetAmount = 2500;

            //----code

            Db.Query().CommandText("delete missionspotinfo where zoneid=@zoneId").SetParameter("@zoneId", _zone.Id).ExecuteNonQuery();

            var staticObjects = MissionSpot.GetStaticObjectsFromZone(_zone);

            var spotInfos = new List<MissionSpot>();

           

            PlaceOneType(spotInfos, MissionSpotType.fieldterminal, fieldTerminalTargetAmount, fieldTerminalDistanceInfos,  staticObjects, fieldTerminalAccuracy);
            PlaceOneType(spotInfos, MissionSpotType.mswitch, switchTargetAmount, switchDistanceInfos,  staticObjects, structureAccuracy);
            PlaceOneType(spotInfos, MissionSpotType.kiosk, kioskTargetAmount, kioskDistanceInfos,  staticObjects, structureAccuracy);
            PlaceOneType(spotInfos, MissionSpotType.itemsupply, itemSupplyTargetAmount, itemSupplyDistanceInfos,   staticObjects, structureAccuracy);
            PlaceOneType(spotInfos, MissionSpotType.randompoint, randomPointTargetAmount, randomPointDistanceInfos, staticObjects, randomPointAccuracy);

            var resultBitmap = DrawResultOnBitmap(spotInfos, staticObjects);

            SendDrawFunctionFinished(request);

            return resultBitmap;
        }

        private static readonly Color _passableColor = Color.FromArgb(255,16, 26, 26);
        private static readonly Color _fieldTerminalColor = Color.White;
        private static readonly Color _switchColor = Color.FromArgb(255,255, 82, 0);
        private static readonly Color _kioskColor = Color.FromArgb(255,153, 206, 70);
        private static readonly Color _itemSupplyColor = Color.FromArgb(255,48, 198, 249);
        private static readonly Color _randomPointColor = Color.FromArgb(255,237, 144, 251);
        private static readonly Color _dockingBaseColor = Color.FromArgb(255,21, 68, 29);
        private static readonly Color _teleportColor = Color.FromArgb(255,74, 78, 6);
        private static readonly Color _sapColor = Color.FromArgb(255,54, 29, 99);
        private static readonly Color _islandColor = Color.FromArgb(255,0, 24, 59);
        private static readonly Color _findArtifactColor = Color.FromArgb(255,255, 204, 77);
        private static readonly Color _popNpcColor = Color.FromArgb(255, 105, 82, 0);
        private static readonly Color _lootColor = Color.FromArgb(255, 0, 151, 208);
        private static readonly Color _fetchItemColor = Color.FromArgb(255, 12, 137, 119);
        private static readonly Color _killColor = Color.FromArgb(255, 152, 15, 15);
        private static readonly Color _scanMineralColor = Color.FromArgb(255, 124, 164, 255);
        private static readonly Color _drillMineralColor = Color.FromArgb(255, 214, 144, 126);
        private static readonly Color _harvestColor = Color.FromArgb(255, 164, 231, 72);

        private Bitmap DrawResultOnBitmap(List<MissionSpot> spotInfos, Dictionary<MissionSpotType, List<Position>> staticObjects )
        {
            var b = _zone.CreatePassableBitmap(_passableColor);

            foreach (var info in spotInfos)
            {
                switch (info.type)
                {
                    case MissionSpotType.fieldterminal:
                        FillEllipseOnPoint(_fieldTerminalColor, 6, info.position, b);
                        break;
                    case MissionSpotType.mswitch:
                        FillEllipseOnPoint(_switchColor, 4, info.position, b);
                        break;
                    case MissionSpotType.kiosk:
                        FillEllipseOnPoint(_kioskColor, 4, info.position, b);
                        break;
                    case MissionSpotType.itemsupply:
                        FillEllipseOnPoint(_itemSupplyColor, 4, info.position, b);
                        break;
                    case MissionSpotType.randompoint:
                        FillEllipseOnPoint(_randomPointColor, 1, info.position, b);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var kvp in staticObjects)
            {
                var spotType = kvp.Key;
                var positions = kvp.Value;
                var objectColor = Color.DarkOliveGreen;
                var radius = 2;

                switch (spotType)
                {
                        case MissionSpotType.terminal:
                        objectColor = Color.FromArgb( 78, 98, 44);
                        radius = 12;
                        break;

                        case MissionSpotType.teleport:
                        objectColor = Color.FromArgb(31, 57, 70);
                        radius = 10;
                        break;

                        case MissionSpotType.sap:
                        objectColor = Color.FromArgb(48, 22, 41);
                        radius = 8;
                        break;

                    default:
                        continue;
                }


                foreach (var position in positions)
                {
                    DrawEllipseOnPoint(objectColor, radius, position, b);
                }
            }


            var nofFt = spotInfos.Count(s => s.type == MissionSpotType.fieldterminal);
            var nofSwitch = spotInfos.Count(s => s.type == MissionSpotType.mswitch);
            var nofKisok = spotInfos.Count(s => s.type == MissionSpotType.kiosk);
            var nofItemsupply = spotInfos.Count(s => s.type == MissionSpotType.itemsupply);
            var nofRndPoint = spotInfos.Count(s => s.type == MissionSpotType.randompoint);

            var fttext = $"{nofFt} field terminal";
            var swtext = $"{nofSwitch} switch";
            var kiotext = $"{nofKisok} kiosk";
            var istext = $"{nofItemsupply} item supply";
            var rptext = $"{nofRndPoint} random point";

            b.WithGraphics(g => g.DrawString(fttext, new Font("Tahoma", 15), new SolidBrush(_fieldTerminalColor), new PointF(20, 40)));
            b.WithGraphics(g => g.DrawString(swtext, new Font("Tahoma", 15), new SolidBrush(_switchColor), new PointF(20, 60)));
            b.WithGraphics(g => g.DrawString(kiotext, new Font("Tahoma", 15), new SolidBrush(_kioskColor), new PointF(20, 80)));
            b.WithGraphics(g => g.DrawString(istext, new Font("Tahoma", 15), new SolidBrush(_itemSupplyColor), new PointF(20, 100)));
            b.WithGraphics(g => g.DrawString(rptext, new Font("Tahoma", 15), new SolidBrush(_randomPointColor), new PointF(20, 120)));

            return b;
        }

        private void FillEllipseOnPoint(Color color, int radius, Position position, Bitmap bitmap)
        {
            var gfx = Graphics.FromImage(bitmap);
            gfx.CompositingQuality = CompositingQuality.HighQuality;
            gfx.SmoothingMode = SmoothingMode.AntiAlias;

            var size = radius * 2;
            var x = position.intX - radius;
            var y = position.intY - radius;

            gfx.FillEllipse(new SolidBrush(color),x,y,size,size );


            /*
            for (var j = position.intY - radius; j <= position.intY + radius; j++)
            {
                for (var i = position.intX - radius; i <= position.intX + radius; i++)
                {
                    if (position.IsInRangeOf2D(i, j, radius))
                        bitmap.SetPixel(i, j, color);
                }
            }
             */
        }


        private void DrawEllipseOnPoint(Color color, int radius, Position position, Bitmap bitmap)
        {
            var gfx = Graphics.FromImage(bitmap);
            gfx.CompositingQuality = CompositingQuality.HighQuality;
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            
            var size = radius * 2;
            var x = position.intX - radius;
            var y = position.intY - radius;

            gfx.DrawEllipse(  new Pen(color,3), x, y, size, size);
            
        }


        private void PlaceOneType(List<MissionSpot> spotInfos, MissionSpotType type, int targetAmount, Dictionary<MissionSpotType, int> distanceInfos, Dictionary<MissionSpotType, List<Position>> staticObjects, AccuracyInfo accuracyInfo)
        {
            var distanceToMyType = distanceInfos[type];
            var zoneWidth = _zone.Size.Width;
            var borderWidthMax = zoneWidth;

            //to make it fast 
            //border increase 200
            //start border 200

            var borderIncrease = accuracyInfo.borderIncrease;
            var currentBorder = accuracyInfo.initialBorder;
            var foundTotal = 0;

            var freePoints = new List<Point>(_zone.Configuration.Size.Width * _zone.Configuration.Size.Height);
            InitPoints(spotInfos, distanceInfos, staticObjects, freePoints);

            while (true)
            {
                if (currentBorder > borderWidthMax)
                {
                    Logger.Info("Max border reached " + type);
                    return;
                }

                Logger.Info("border:" + currentBorder);

                var maxAttempts = freePoints.Count;
                var attempts = 0;
                while (true)
                {
                    if (freePoints.Count == 0)
                    {
                        Logger.Info("no more free points");
                        return;
                    }

                    attempts ++;
                    if (attempts > maxAttempts)
                    {
                        currentBorder += borderIncrease;
                        Logger.Info("MAX attempts reached!");
                        break;
                    }

                    if (attempts % 50000 == 0)
                    {
                        Logger.Info(type + " working " + attempts);
                    }

                    var pickedIndex = FastRandom.NextInt(freePoints.Count - 1);
                    var pickedPosition = freePoints[pickedIndex].ToPosition();

                    

                    //super turbo mode, skips border check when it generates random points
                    //if (type != MissionSpotType.randompoint)
                    //{ is keep distance }


                    //good if the distance is kept
                        if (!IsKeepDistance(pickedPosition, spotInfos, distanceInfos, staticObjects, 0, currentBorder))
                        {
                            continue;
                        }
                    

                    if (!CheckConditionsAroundPosition(pickedPosition, accuracyInfo.blockRadius, accuracyInfo.islandRadius))
                    {
                        freePoints.Remove(pickedPosition);
                        continue;
                    }

                    //--- yay! position found!

                    var si = new MissionSpot(type, pickedPosition, _zone.Id);

                    spotInfos.Add(si);
                    SaveInfoAsync(si);

                    foundTotal++;
                    if (foundTotal >= targetAmount)
                    {
                        Logger.Info(foundTotal + " " + type + " was found successfully. Target amount reached!");
                        return;
                    }

                    CleanUpOneSpot(pickedPosition, distanceToMyType, ref freePoints);
                    //MakeSnap(type,freeKeys);
                    Logger.Info(type + "\t\t" + foundTotal + "\tattempts:" + attempts);
                    break;
                }
            }
        }

        private static void SaveInfoAsync(MissionSpot si)
        {
            Task.Run(() => { si.Save(); });
        }

        private void InitPoints(List<MissionSpot> spotInfos, Dictionary<MissionSpotType, int> distanceInfos, Dictionary<MissionSpotType, List<Position>> staticObjects, List<Point> freePoints)
        {
            var zoneWidth = _zone.Size.Width;
            var zoneHeight = _zone.Size.Height;

            for (var j = 0; j < zoneHeight; j++)
            {
                for (var i = 0; i < zoneWidth; i++)
                {
                    var p = new Position(i, j);
                    if (!_zone.Terrain.IsPassable(p))
                        continue;

                    if (IsAnySpotWithin(p, spotInfos, distanceInfos, staticObjects))
                        continue;

                    freePoints.Add(new Point(i, j));
                }
            }

            freePoints.TrimExcess();
        }

        private static void CleanUpOneSpot(Position center, int distance, ref List<Point> freePoints)
        {
            var goodKeys = new List<Point>(freePoints.Count);
            foreach (var point in freePoints)
            {
                var pos = point.ToPosition();
                if (!pos.IsInRangeOf2D(center, distance))
                {
                    goodKeys.Add(point);
                }
            }

            freePoints = goodKeys;

            Logger.Info("points after cleanup:" + freePoints.Count);
        }

        private static bool IsAnySpotWithin(Position position, List<MissionSpot> spotInfos, Dictionary<MissionSpotType, int> distanceInfos, Dictionary<MissionSpotType, List<Position>> staticObjects)
        {
            foreach (var kvp in staticObjects)
            {
                var staticObjectType = kvp.Key;
                var positions = kvp.Value;

                var distance = distanceInfos[staticObjectType];

                foreach (var staticObjectPos in positions)
                {
                    if (staticObjectPos.IsInRangeOf2D(position, distance))
                    {
                        return true;
                    }
                }
            }

            foreach (var spotInfo in spotInfos)
            {
                var distance = distanceInfos[spotInfo.type];

                if (position.IsInRangeOf2D(spotInfo.position, distance))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsKeepDistance(Position position, List<MissionSpot> spotInfos, Dictionary<MissionSpotType, int> distanceInfos, Dictionary<MissionSpotType, List<Position>> staticObjects, int currentBorderMin, int currentBorderMax)

        {
            //initial state, choose one point close to static objects

            foreach (var kvp in staticObjects)
            {
                var staticObjectType = kvp.Key;
                var staticObjectPositions = kvp.Value;

                var distance = distanceInfos[staticObjectType];

                var distanceMin = distance + currentBorderMin;
                var distanceMax = distance + currentBorderMax;

                foreach (var staticObjectPos in staticObjectPositions)
                {
                    if (position.IsInRangeOf2D(staticObjectPos, distanceMax) && !position.IsInRangeOf2D(staticObjectPos, distanceMin))
                    {
                        return true;
                    }
                }
            }

            foreach (var spotInfo in spotInfos)
            {
                var distance = distanceInfos[spotInfo.type];
                var distanceMax = distance + currentBorderMax;
                var distanceMin = distance + currentBorderMin;

                if (position.IsInRangeOf2D(spotInfo.position, distanceMax))
                {
                    if (!position.IsInRangeOf2D(position, distanceMin))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     ide lehet berakosgatni meg radiusokat es felteteleket, pl island radius
        /// </summary>
        private bool CheckConditionsAroundPosition(Position center, int blockRadius, int islandRadius, bool validationMode = false)
        {
            if (validationMode)
            {
                blockRadius = (blockRadius - 2).Clamp(0, int.MaxValue);
                islandRadius = (islandRadius - 2).Clamp(0, int.MaxValue);
            }

            var maximalRadius = blockRadius.Max(islandRadius);

            //1x megyunk csak vegig !!!

            for (var j = center.intY - maximalRadius; j < center.intY + maximalRadius; j++)
            {
                for (var i = center.intX - maximalRadius; i < center.intX + maximalRadius; i++)
                {
                    var p = new Position(i, j);
                    if (!p.IsValid(_zone.Size))
                        continue;

                    // a nagyobb radiussal erdemes kezdeni, hamarabb kilep

                    if (center.IsInRangeOf2D(p, islandRadius))
                    {
                        var bi = _zone.Terrain.Blocks.GetValue(i, j);
                        if (bi.Flags.HasFlag(BlockingFlags.Island))
                            return false;
                    }

                    if (center.IsInRangeOf2D(p, blockRadius))
                    {
                        if (!_zone.Terrain.IsPassable(p))
                            return false;

                        var tf = _zone.Terrain.Controls.GetValue(p);
                        if (tf.IsAnyHighway)
                            return false;
                    }
                }
            }

            return true;
        }

        private int _counter;

        private void MakeASnapshot(MissionSpotType spotType, List<Point> freePoints)
        {
            var pointsCopy = new List<Point>(freePoints);

            _counter++;
            var fileName = spotType + "_freepoints." + $"{_counter:0000}";

            Task.Run(() =>
            {
                var bmp = _zone.CreateBitmap();

                foreach (var point in pointsCopy)
                {
                    bmp.SetPixel(point.X, point.Y, Color.White);
                }

                _saveBitmapHelper.SaveBitmap(_zone,bmp, fileName);
            });
        }


        
    }
}
