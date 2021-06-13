using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.IO;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.RequestHandlers.Zone.StatsMapDrawing
{
    public partial class ZoneDrawStatMap : IRequestHandler<IZoneRequest>
    {
        private IZone _zone;
        private readonly IFileSystem _fileSystem;
        private IRequest _request;
        private readonly SaveBitmapHelper _saveBitmapHelper;
        private readonly MissionDataCache _missionDataCache;
        private readonly Dictionary<string, Action<IRequest>> _actions = new Dictionary<string, Action<IRequest>>();
        private string _typeString;
        private bool _sendtoclient;

        public ZoneDrawStatMap(IFileSystem fileSystem,SaveBitmapHelper saveBitmapHelper,MissionDataCache missionDataCache)
        {
            _fileSystem = fileSystem;
            _saveBitmapHelper = saveBitmapHelper;
            _missionDataCache = missionDataCache;

            _actions["minerals"] = r => CreateMineralBitmaps();
            RegisterCreator("npc", CreateNPCMap);
            RegisterCreator("altitude", CreateAltitudeBitmap);
            RegisterCreator("slope", CreateSlopeBitmap);
            RegisterCreator("missionTarget", CreateMissionTargetsMap);
            RegisterCreator("decorblock", CreateDecorBlockingMap);
            RegisterCreator("electroplant", CreateElectroPlantMap);
            RegisterCreator("structures", CreateStructuresMap);
            RegisterCreator("players", CreatePlayersMap);
            RegisterCreator("wall", CreateWallMap);
            RegisterCreator("wallpossible", CreateWallPossibleMap);
            RegisterCreator("wallplaces", CreateWallPlaces);
            RegisterCreator("passable", () => _zone.CreatePassableBitmap(Color.White));
            RegisterCreator("islandmask", CreateIslandMaskMap);
            RegisterCreator("controlmap", CreateControlMap);
            RegisterCreator("TerraformProtected", CreateControlFlagMap(TerrainControlFlags.TerraformProtected));
            RegisterCreator("PBSTerraformProtected", CreateControlFlagMap(TerrainControlFlags.PBSTerraformProtected));
            RegisterCreator("Roaming", CreateControlFlagMap(TerrainControlFlags.Roaming));
            RegisterCreator("AntiPlant", CreateControlFlagMap(TerrainControlFlags.AntiPlant));
            RegisterCreator("Highway", CreateControlFlagMap(TerrainControlFlags.Highway));
            RegisterCreator("ConcreteA", CreateControlFlagMap(TerrainControlFlags.ConcreteA));
            RegisterCreator("ConcreteB", CreateControlFlagMap(TerrainControlFlags.ConcreteB));
            RegisterCreator("NpcRestricted", CreateControlFlagMap(TerrainControlFlags.NpcRestricted));
            RegisterCreator("PBSHighway", CreateControlFlagMap(TerrainControlFlags.PBSHighway));
            RegisterCreator("SyndicateArea", CreateControlFlagMap(TerrainControlFlags.SyndicateArea));
            RegisterCreator("HighWayCombo", CreateControlFlagMap(TerrainControlFlags.HighWayCombo));
            RegisterCreator("TerraformProtectedCombo", CreateControlFlagMap(TerrainControlFlags.TerraformProtectedCombo));
            RegisterCreator("block",CreateBlockingMap);
            RegisterCreator("plants",CreatePlantsMap);
            RegisterCreator("placemo",GenerateMissionSpots );
            RegisterCreator("validatemobjects",ValidateMissionObjectLocations);
            RegisterCreator("rndpointsonly", GenerateRandomPointsOnly);
            RegisterCreator("displayspots", DisplaySpots);
            RegisterCreator("worstspots",DrawWorstSpotsMap);
            RegisterCreator("alltargets", DrawAllTargetsOnZone);
            RegisterCreator(k.groundType, CreateGroundTypeMap);
        }

        private void RegisterCreator(string type,Func<IRequest,Bitmap> bitmapFactory)
        {
            _actions[type] = (r) => CreateAndSave(type,() =>  bitmapFactory(r));
        }

        private void RegisterCreator(string type,Func<Bitmap> bitmapFactory)
        {
            _actions[type] = (r) => CreateAndSave(type, bitmapFactory);
        }

        private void CreateAndSave(string postfix, Func<Bitmap> bitmapFactory)
        {
            var bmp = bitmapFactory();
            if (bmp == null)
                return;

            bmp.WithGraphics(g => g.DrawString(_zone.Configuration.Name, new Font("Tahoma", 20), Brushes.Red, new PointF(10, 10)));
            var fileName = "stat_" + postfix;

            if (_sendtoclient) // send to client.
            {
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    var Base64 = Convert.ToBase64String(ms.GetBuffer());
                    Message.Builder.FromRequest(_request).SetData("name", fileName).SetData("img", Base64).Send();
                }
            }
            else // save locally.
            {
                _saveBitmapHelper.SaveBitmap(_zone, bmp, fileName);
            }
        }

        public void HandleRequest(IZoneRequest request)
        {
            _zone = request.Zone;
            _request = request;
            var type = request.Data.GetOrDefault<string>(k.type);
            bool sendtoclient = request.Data.GetOrDefault<int>("sendtoclient").ToBool();
            _typeString = type; //save for later use
            _sendtoclient = sendtoclient;

            var action = _actions.GetOrDefault(type);
            if (action != null)
            {
                Task.Run(() =>
                {
                    //bitmappel ternek vissza, a regisztraltak

                    var now = GlobalTimer.Elapsed.TotalMilliseconds;
                    action(request);
                    var end = GlobalTimer.Elapsed.TotalMilliseconds;

                    Logger.Info( "draw stats map type:[" + type + "] execution seconds:" + Math.Round((end-now)/1000,4));
                });
            }
            else
            {
                //minden mas ami nem bitmappel ter vissza
                switch (type)
                {
                    case "teleportdecor":
                    {
                        CreateTeleportDecorMaps();
                        break;
                    }
                    case "barrier":
                    {
                        break;
                    }
                    case "mbl":
                    {
                        CreateMissionMapByLevels();
                        break;
                    }
                    case "pbshighway":
                    {
                        GenerateNewFlagsMap();
                        break;
                    }
                    case "targetlog":
                    {
                        DrawMissionTargetLog(request);
                        break;
                    }
                    case "PlantBonsai":
                    {
                        CreatePlantMap(PlantType.Bonsai);
                        break;
                    }
                    case "PlantBushA":
                    {
                        CreatePlantMap(PlantType.BushA);
                        break;
                    }
                    case "PlantBushB":
                    {
                        CreatePlantMap(PlantType.BushB);
                        break;
                    }
                    case "PlantDevrinol":
                    {
                        CreatePlantMap(PlantType.Devrinol);
                        break;
                    }
                    case "PlantGrassA":
                    {
                        CreatePlantMap(PlantType.GrassA);
                        break;
                    }
                    case "PlantGrassB":
                    {
                        CreatePlantMap(PlantType.GrassB);
                        break;
                    }
                    case "PlantNanoWheat":
                    {
                        CreatePlantMap(PlantType.NanoWheat);
                        break;
                    }
                    case "PlantPineTree":
                    {
                        CreatePlantMap(PlantType.PineTree);
                        break;
                    }
                    case "PlantPoffeteg":
                    {
                        CreatePlantMap(PlantType.Poffeteg);
                        break;
                    }
                    case "PlantQuag":
                    {
                        CreatePlantMap(PlantType.Quag);
                        break;
                    }
                    case "PlantRango":
                    {
                        CreatePlantMap(PlantType.Rango);
                        break;
                    }
                    case "PlantReed":
                    {
                        CreatePlantMap(PlantType.Reed);
                        break;
                    }
                    case "PlantRustBush":
                    {
                        CreatePlantMap(PlantType.RustBush);
                        break;
                    }
                    case "PlantSlimeRoot":
                    {
                        CreatePlantMap(PlantType.SlimeRoot);
                        break;
                    }
                    case "PlantTitanPlant":
                    {
                        CreatePlantMap(PlantType.TitanPlant);
                        break;
                    }
                    case "PlantTreeIron":
                    {
                        CreatePlantMap(PlantType.TreeIron);
                        break;
                    }
                }
            }

            var data = new Dictionary<string, object>
            {
                {"note", type + " async started. "}
            };
            
            Message.Builder.FromRequest(request).WithData(data).Send();
        }

        private Bitmap CreateAltitudeBitmap()
        {
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var altitudeValue = (byte)(_zone.Terrain.Altitude.GetAltitudeAsDouble(x, y) / 2048 * 612).Clamp(0, 255);
                var color = Color.FromArgb(altitudeValue, altitudeValue, altitudeValue);
                bmp.SetPixel(x, y, color);
            });
        }

        private Bitmap CreateSlopeBitmap()
        {
            const int threshold = 4 * 4;

            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var slope = _zone.Terrain.Slope.GetValue(x, y);
                var block = _zone.Terrain.Blocks.GetValue(x, y);

                if (block.Flags > 0 || slope >= threshold) 
                    return;

                var c = 255 - (int)(((double)slope / threshold) * 255);
                bmp.SetPixel(x, y,Color.FromArgb(c,c,c));
            });
        }

        private Bitmap CreateBlockingMap()
        {
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var b = _zone.Terrain.IsBlocked(x, y);
                if ( !b )
                    return;

                bmp.SetPixel(x,y,Color.White);
            });
        }

        private void GenerateNewFlagsMap()
        {
            CreateAndSave("newflags_", CreateNewFlagsMap);
        }


        private Bitmap CreateNewFlagsMap()
        {
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var ci = _zone.Terrain.Controls.GetValue(x, y);
                
                var r = 0;
                var g = 0;
                var b = 0;
                
                if (ci.PBSHighway)
                {
                    r = 255;
                }

                if (ci.Highway)
                {
                    g = 255;
                }

                if (ci.PBSTerraformProtected)
                {
                    b = 255;
                }

                var color = Color.FromArgb(255, r, g, b);

                bmp.SetPixel(x, y, color);
            });
        }

        private Bitmap CreatePlayersMap()
        {
            return CreateAltitudeBitmap().WithGraphics(g =>
            {
                foreach (var unit in _zone.GetCharacters())
                {
                    var size = 12;
                    var pen = Pens.Red;

                    var x = unit.GetPlayerRobotFromZone().CurrentPosition.intX - (size / 2);
                    var y = unit.GetPlayerRobotFromZone().CurrentPosition.intY - (size / 2);

                    g.DrawEllipse(pen, x, y, size, size);
                    const int width = 4;
                    g.DrawEllipse(Pens.BlueViolet, x, y, width, width);
                    g.DrawString(unit.Nick, new Font("Tahoma", 12), Brushes.Red, x + 10, y + 10);
                }
            });
        }

        private Bitmap CreateNPCMap()
        {
            return CreateAltitudeBitmap().WithGraphics(g =>
            {
                DrawNpcPresencesOnGraphic(g);
                DrawNpcFlocksOnGraphic(g);
            });
        }

        private void DrawNpcPresencesOnGraphic(Graphics graphics)
        {
            foreach (var presence in _zone.PresenceManager.GetPresences().OfType<RoamingPresence>())
            {
                graphics.DrawRectangle(Pens.Blue, presence.Area.X1, presence.Area.Y1, presence.Area.Width, presence.Area.Height);
                graphics.DrawString(presence.Configuration.Name, new Font("Tahoma", 8), Brushes.Red, presence.Area.X1, presence.Area.Y1);
            }
        }

        private void DrawNpcFlocksOnGraphic(Graphics graphics)
        {
            foreach (var flock in _zone.PresenceManager.GetPresences().OfType<RoamingPresence>().SelectMany(p => p.Flocks))
            {
                var txSpawnMax = flock.Configuration.SpawnOrigin.intX - flock.Configuration.SpawnRange.Max;
                var tySpawnMax = flock.Configuration.SpawnOrigin.intY - flock.Configuration.SpawnRange.Max;
                var widthSpawnMax = flock.Configuration.SpawnRange.Max * 2;

                var txHomeRange = flock.Configuration.SpawnOrigin.intX - flock.HomeRange;
                var tyHomeRange = flock.Configuration.SpawnOrigin.intY - flock.HomeRange;
                var widthHome = flock.HomeRange * 2;

                graphics.DrawEllipse(Pens.BlueViolet, txSpawnMax, tySpawnMax, widthSpawnMax, widthSpawnMax);
                graphics.DrawEllipse(Pens.Red, txHomeRange, tyHomeRange, widthHome, widthHome);
                graphics.DrawString(flock.Configuration.Name, new Font("Tahoma", 10), Brushes.Red, txSpawnMax, tySpawnMax);
            }
        }

        private Bitmap CreateMissionTargetsMap()
        {
            return CreateAltitudeBitmap().WithGraphics(DrawMissionTargetsOnGraphics);
        }

        private void DrawMissionTargetsOnGraphics(Graphics graphics)
        {
            var targets = _missionDataCache.GetAllMissionTargets.Where(t => t.ValidZoneSet && t.ZoneId == _zone.Id).ToList();

            var targetsStr = targets.Select(m => m.id).ArrayToString();
            var targetRecords = Db.Query().CommandText("select id,name from missiontargets where id in (" + targetsStr + ")").Execute();
            var targetNames = targetRecords.ToDictionary(targetRecord => targetRecord.GetValue<int>(0), targetRecord => targetRecord.GetValue<string>(1));

            foreach (var missionTarget in targets)
            {
                if (!missionTarget.ValidRangeSet)
                {
                    Logger.Error("consistency error in mission target. " + missionTarget);
                    continue;
                }

                var tx = missionTarget.targetPosition.intX - 2;
                var ty = missionTarget.targetPosition.intY - 2;
                const int width = 4;
                graphics.DrawEllipse(Pens.BlueViolet, tx, ty, width, width);
                graphics.DrawString(targetNames[missionTarget.id], new Font("Tahoma", 8), Brushes.White, tx, ty);
            }
        }



        private Bitmap CreateDecorBlockingMap()
        {
            return CreateAltitudeBitmap().ForEach((bmp, x, y) =>
            {
                var blockingInfo = _zone.Terrain.Blocks.GetValue(x, y);
                if (!blockingInfo.Decor) 
                    return;

                var color = blockingInfo.Height > 0 ? Color.Green : Color.Orange;
                bmp.SetPixel(x, y,color);
            });
        }

        private Bitmap CreateElectroPlantMap()
        {
            return CreateAltitudeBitmap().ForEach((bmp, x, y) =>
            {
                var plantInfo = _zone.Terrain.Plants.GetValue(x, y);

                if (plantInfo.type != PlantType.ElectroPlant)
                    return;

                var r = ((255 / 5) * (plantInfo.state)).Clamp(0, 255);
                var color = Color.FromArgb(255, r, 128, 0);

                if (plantInfo.state == 0)
                {
                    color = Color.FromArgb(255, 255, 0, 30);
                }

                bmp.SetPixel(x, y, color);
            });

        }

        private Bitmap CreatePlantMap(PlantType plantType)
        {
            return CreateAltitudeBitmap().ForEach((bmp, x, y) =>
            {
                var plantInfo = _zone.Terrain.Plants.GetValue(x, y);

                if (plantInfo.type != plantType)
                    return;

                var r = ((255 / 5) * (plantInfo.state)).Clamp(0, 255);
                var color = Color.FromArgb(255, r, 128, 0);

                if (plantInfo.state == 0)
                {
                    color = Color.FromArgb(255, 255, 0, 30);
                }

                bmp.SetPixel(x, y, color);
            });

        }

        private Bitmap CreatePlantsMap()
        {
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var plantInfo = _zone.Terrain.Plants.GetValue(x, y);

                if (plantInfo.type == PlantType.NotDefined)
                    return;

                if (plantInfo.state == 0)
                    return;

                bmp.SetPixel(x, y, Color.White);
            });
        }

        private Bitmap CreateStructuresMap()
        {
            return _zone.CreateBitmap().WithGraphics(g =>
            {
                foreach (var unit in _zone.GetStaticUnits())
                {
                    var size = 3;
                    var pen = Pens.White;

                    if (unit.IsCategory(CategoryFlags.cf_outpost))
                    {
                        pen = Pens.LightSeaGreen;
                        size = 150;
                    }
                    else if (unit.IsCategory(CategoryFlags.cf_public_docking_base))
                    {
                        pen = Pens.Yellow;
                        size = 150;
                    }
                    else if (unit.IsCategory(CategoryFlags.cf_teleport_column))
                    {
                        pen = Pens.WhiteSmoke;
                        size = 100;
                    }

                    var x = unit.CurrentPosition.intX - (size / 2);
                    var y = unit.CurrentPosition.intY - (size / 2);

                    g.DrawEllipse(pen, x, y, size, size);
                }
            });
        }

        private Bitmap CreateWallMap()
        {
            return CreateAltitudeBitmap().ForEach((bmp, x, y) =>
            {
                var pInfo = _zone.Terrain.Plants.GetValue(x, y);
                if (pInfo.type != PlantType.Wall)
                    return;

                var r = ((255 / 11) * (pInfo.state)).Clamp(0, 255);
                var g = pInfo.health;
                var pColor = Color.FromArgb(255, r, g, 0);
                bmp.SetPixel(x, y, pColor);
            });
        }

        private Bitmap CreateWallPossibleMap()
        {
            var outposts = _zone.Units.OfType<Outpost>().ToArray();
            var teleports = _zone.Units.OfType<Teleport>().ToArray();

            var sapinfos = outposts.SelectMany(o => o.SAPInfos).ToArray();

            return CreateAltitudeBitmap().ForEach((bmp, x, y) =>
            {
                var plantInfo = _zone.Terrain.Plants.GetValue(x, y);

                var allowed = true;

                var outpostNear = outposts.Any(o => o.CurrentPosition.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MIN_DISTANCE_FROM_BASE));
                var teleportNear = teleports.Any(t => t.CurrentPosition.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MIN_DISTANCE_FROM_BASE));
                var sapNear = sapinfos.Any(s => s.Position.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MIN_DISTANCE_FROM_SAP));

                if (outpostNear || teleportNear || sapNear)
                {
                    allowed = false;
                }
                else
                {
                    var outpostInWallRange = outposts.Any(o => o.CurrentPosition.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MAX_DISTANCE_FROM_OUTPOST));

                    if (!outpostInWallRange)
                    {
                        allowed = false;
                    }
                }

                if (!allowed && plantInfo.type != PlantType.Wall)
                    return;

                var pixel = bmp.GetPixel(x, y);

                var r = pixel.R;
                var g = pixel.G;
                var b = pixel.B;

                if (allowed)
                {
                    r = (byte)(r + 40).Clamp(0, 255);
                }

                if (plantInfo.type == PlantType.Wall)
                {
                    r = 0;
                    g = plantInfo.health;
                    b = 0;
                }

                bmp.SetPixel(x, y, Color.FromArgb(255, r, g, b));
            });

        }

        private Bitmap CreateWallPlaces()
        {
            var outposts = _zone.Units.OfType<Outpost>().ToArray();
            var teleports = _zone.Units.OfType<Teleport>().ToArray();

            var sapinfos = outposts.SelectMany(o => o.SAPInfos).ToArray();

            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var allowed = true;

                var outpostNear = outposts.Any(o => o.CurrentPosition.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MIN_DISTANCE_FROM_BASE));
                var teleportNear = teleports.Any(t => t.CurrentPosition.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MIN_DISTANCE_FROM_BASE));
                var sapNear = sapinfos.Any(s => s.Position.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MIN_DISTANCE_FROM_SAP));

                if (outpostNear || teleportNear || sapNear)
                {
                    allowed = false;
                }
                else
                {
                    var outpostInWallRange = outposts.Any(o => o.CurrentPosition.IsInRangeOf2D(x, y, DistanceConstants.PLANT_MAX_DISTANCE_FROM_OUTPOST));
                    if (!outpostInWallRange)
                    {
                        allowed = false;
                    }
                }

                if (allowed)
                {
                    bmp.SetPixel(x, y, Color.FromArgb(255, 255, 0, 0));
                }
            });
        }

        private Bitmap CreateIslandMaskMap()
        {
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var blockingInfo = _zone.Terrain.Blocks.GetValue(x, y);
                if (!blockingInfo.Island)
                    return;

                bmp.SetPixel(x, y, Color.White);
            });
        }

        private Func<Bitmap> CreateControlFlagMap(TerrainControlFlags flag)
        {
            return () =>
            {
                return _zone.CreateBitmap().ForEach((bmp, x, y) =>
                {
                    var control = _zone.Terrain.Controls.GetValue(x, y);
                    if (!control.Flags.HasFlag(flag))
                        return;
                    bmp.SetPixel(x, y, Color.White);
                });
            };
        }


        private Bitmap CreateControlMap()
        {
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var control = _zone.Terrain.Controls.GetValue(x, y);
                var c = (int)control.Flags;
                bmp.SetPixel(x, y, Color.FromArgb(c,c,c));
            });
        }

        private Bitmap CreateGroundTypeMap()
        {
            var numGroundTypes = Enum.GetNames(typeof(GroundType)).Length;
            var colors = new Color[numGroundTypes];
            var random = new Random(numGroundTypes);
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
            }
            return _zone.CreateBitmap().ForEach((bmp, x, y) =>
            {
                var groundType = _zone.Terrain.Plants.GetValue(x, y).groundType;
                var c = colors[((int)groundType).Clamp(0, numGroundTypes - 1)];
                bmp.SetPixel(x, y, c);
            });
        }

        private void CreateMineralBitmaps()
        {
            foreach (var mineralLayer in _zone.Terrain.Materials.OfType<MineralLayer>())
            {
                var postfix = mineralLayer.Type.ToString();
                CreateAndSave("mineral_" + postfix,() =>  CreateMineralsToNormalizedBitmap(mineralLayer));
            }
        }

        [CanBeNull]
        private Bitmap CreateMineralsToNormalizedBitmap(MineralLayer layer)
        {
            var bitmap = _zone.CreatePassableBitmap(_passableColor);

            foreach (var node in layer.Nodes)
            {
                var maxAmount = node.GetMaxAmount();

                for (int y = node.Area.Y1; y <= node.Area.Y2 ; y++)
                {
                    for (int x = node.Area.X1; x <= node.Area.X2; x++)
                    {
                        var n = (double)node.GetValue(x, y) / maxAmount;
                        if (n > 0.0)
                        {
                            var c = (int)(n * 255);
                            bitmap.SetPixel(x, y, Color.FromArgb(c,0,0));
                        }
                    }
                  
                }
            }

            return bitmap.WithGraphics(g =>
            {
                var infoString = $"{layer.Type}";
                g.DrawString(infoString, new Font("Tahoma", 10), Brushes.White, 10, 10);
            });
        }

        private void CreateTeleportDecorMaps()
        {
            Bitmap bitmap;
            Bitmap circlesBitmap;
            CreateTeleportDecorMaps(out bitmap,out circlesBitmap);

            CreateAndSave("teleportdecor", () => bitmap);
            CreateAndSave("teleportdecor_circles", () => circlesBitmap);
        }

        /// <summary>
        /// This function creates the blend map on gamma islands around the teleports
        /// Finds the farthest decor tile and draws a smooth circle gradient around the teleport
        /// </summary>
        private void CreateTeleportDecorMaps(out Bitmap bitmap,out Bitmap circlesBitmap)
        {
            bitmap = null;
            circlesBitmap = null;
            if (!_zone.Configuration.Terraformable)
                return;

            var terrain = _zone.Terrain;

            bitmap = _zone.CreateBitmap();
            var color = Color.MediumVioletRed;
            var font = new Font("Tahoma", 8);
            var graphics = Graphics.FromImage(bitmap);

            circlesBitmap = _zone.CreateBitmap();
            var circlesGraphics = Graphics.FromImage(circlesBitmap);

            var blendData = _zone.Size.CreateArray<ushort>();

            foreach (var td in _zone.Units.GetAllByCategoryFlags(CategoryFlags.cf_teleport_column))
            {
                var area = Area.FromRadius(td.CurrentPosition, 200);

                var maximumDistance = 0.0;

                var tmpBmp = bitmap;
                area.ForEachXY((x, y) =>
                {
                    if (x < 0 || x >= _zone.Size.Width || y < 0 || y >= _zone.Size.Height) return;

                    var blockInfo = terrain.Blocks.GetValue(x, y);
                    if (!blockInfo.Decor)
                        return;

                    var currentDistance = td.CurrentPosition.TotalDistance2D(new Position(x, y));
                    if (currentDistance > maximumDistance)
                    {
                        maximumDistance = currentDistance;
                    }

                    tmpBmp.SetPixel(x, y, color);
                });

                graphics.DrawString(maximumDistance.ToString(CultureInfo.InvariantCulture), font, Brushes.White, (float)td.CurrentPosition.X, (float)td.CurrentPosition.Y);

                if (maximumDistance <= 0)
                    continue;

                circlesGraphics.FillEllipse(Brushes.White, (float)(td.CurrentPosition.intX - maximumDistance), (float)(td.CurrentPosition.intY - maximumDistance), (float)(maximumDistance * 2), (float)(maximumDistance * 2));

                var tpArea = Area.FromRadius(td.CurrentPosition, (int)maximumDistance + 200);
                tpArea.ForEachXY((x, y) =>
                {
                    if (x < 0 || x >= _zone.Size.Width || y < 0 || y >= _zone.Size.Height)
                        return;

                    var nearRadius = (int)maximumDistance + 4;
                    var farRadius = (int)maximumDistance + 296;
                    double originX = td.CurrentPosition.intX;
                    double originY = td.CurrentPosition.intY;

                    var ratio = MathHelper.DistanceFalloff(nearRadius, farRadius, originX, originY, x, y);
                    if (ratio <= 0)
                        return;

                    var blendValue = (ushort)(ushort.MaxValue * ratio);
                    blendData[x + y * _zone.Size.Width] = blendValue;
                });

                var filename = $"altitude_blend.{_zone.Id:0000}.bin";
                File.WriteAllBytes(@"c:\" + filename, blendData.ToByteArray());
            }
        }

        private void CreateMissionMapByLevels()
        {
            CreateAndSave("mbl", DrawMissionByLevels);
        }



        private Bitmap DrawMissionByLevels()
        {
            var b = CreateAltitudeBitmap();

            DrawPixels(b);

            b.WithGraphics(DrawLayers);
            
            return b;

           
        }

        private void DrawLayers(Graphics g)
        {
            DrawStringTopLeft(g,"valami cucc rajta");
            //... tobbi graphics piszkalo
        }

        private void DrawPixels(Bitmap bitmap)
        {
            DrawPassableInGreen(bitmap);
            //... tobbi bitmap piszkalo
        }


        private void DrawPassableInGreen(Bitmap bmp)
        {
            bmp.ForEach((b, x, y) =>
            {

                var blockedColor = Color.FromArgb(255, 0, 0, 0);
                var passableColor = Color.FromArgb(255, 60, 60, 60);

                if (_zone.Terrain.IsPassable(new Position(x, y)))
                {
                    b.SetPixel(x,y,passableColor);
                }
                else
                {
                    b.SetPixel(x,y,blockedColor);
                }
            });

        }

        private void DrawStringTopLeft(Graphics graphics, string text)
        {
            graphics.DrawString(text, new Font("Tahoma", 20), Brushes.Chocolate, new PointF(50, 100));
        }
        
        private void SendDrawFunctionFinished(IRequest request)
        {
            var info = new Dictionary<string, object> {{k.done, _typeString}};

            Message.Builder.FromRequest(request).WithData(info).Send();
        }

        private void SendBitmapFinished(IRequest request,string name)
        {
            var info = new Dictionary<string, object> { { k.done, _typeString + " " + name } };

            Message.Builder.FromRequest(request).WithData(info).Send();
        }
    }
}