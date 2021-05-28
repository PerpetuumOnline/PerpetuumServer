using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.PBS.ArmorRepairers;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.ControlTower;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.EffectNodes;
using Perpetuum.Zones.PBS.ProductionNodes;
using Perpetuum.Zones.PBS.Turrets;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones.PBS
{
    public static class PBSHelper
    {
        public static IProductionDataAccess ProductionDataAccess { get; set; }
        public static ProductionManager ProductionManager { get; set; }
        public static ItemDeployerHelper ItemDeployerHelper { get; set; }

        private const int MAX_BASES_PER_CORP_PER_ZONE = 3;


        public static bool IsOfflineOnReinforce(Unit pbsUnit)
        {
            return pbsUnit is PBSEffectSupplier || 
                   pbsUnit is PBSTurret || 
                   pbsUnit is PBSArmorRepairerNode;
        }

        public static bool IsPlaceableOutsideOfBase(CategoryFlags categoryFlags)
        {
            return
                categoryFlags.IsAny(new[]
                {
                    CategoryFlags.cf_pbs_mining_towers,
                    CategoryFlags.cf_pbs_energy_well,
                    CategoryFlags.cf_pbs_control_tower,
                    CategoryFlags.cf_pbs_highway_node
                });
        }

        public static int GetCapsuleDefinitionByPBSObject(IPBSObject pbsNode)
        {
            var pbsUnit = pbsNode as Unit;

            if (pbsUnit == null)
            {
                Logger.Error("consistency error. pbs not is not a unit");
                return 0;
            }

            return GetCapsuleDefinitionByPBSObjectDefinition(pbsUnit.Definition);

        }

        public static int GetPBSObjectDefinitionFromCapsule(EntityDefault capsuleDefault)
        {
            var objectDefinition = capsuleDefault.Config.targetDefinition ?? 0;

            if (objectDefinition == 0)
            {
                return -1;
            }

            var objectEd = EntityDefault.Get(objectDefinition);

            if (objectEd.Config.targetDefinition == null)
            {
                return -1;
            }

            return (int) objectEd.Config.targetDefinition;
        }

        public static int GetCapsuleDefinitionByPBSObjectDefinition(int nodeDefinition)
        {

            var pbsEggDefinition = ItemDeployerHelper.GetDeployerItemDefinition(nodeDefinition);

            var capsuleDefinition = ItemDeployerHelper.GetDeployerItemDefinition(pbsEggDefinition);

            if (capsuleDefinition == 0)
            {
                Logger.Error("consistency error. no capsule definition was found for: " + nodeDefinition + " " +
                             EntityDefault.Get(nodeDefinition).Name);
            }

            Debug.Assert(capsuleDefinition > 0,"capsule definition was not found");

            return capsuleDefinition;

        }

        public static ErrorCodes ValidatePBSDockingBasePlacement(IZone zone, Position position, long owner,
            EntityDefault dockingbasEntityDefault)
        {
            //only a set number of bases for one corporation
            var baseCountPerCorporation = zone.Units.Count(u => u is PBSDockingBase && u.Owner == owner);

            if (baseCountPerCorporation + 1 > MAX_BASES_PER_CORP_PER_ZONE)
            {
                return ErrorCodes.MaxDockingBasePerZonePerCorporationReached;
            }

            //current total number of bases per zone
            var baseCountPerZone = zone.Units.Count(u => u is PBSDockingBase);

            //the zone allows
            var maxBasesPerZone = zone.Configuration.MaxDockingBase;

            if (baseCountPerZone + 1 > maxBasesPerZone)
            {
                return ErrorCodes.MaxDockingBasePerZoneReached;
            }

            var typeExclusiveRange = dockingbasEntityDefault.Config.typeExclusiveRange;

            if (typeExclusiveRange == null)
            {
                Logger.Error("no typeExclusiveRange defined for " + dockingbasEntityDefault);
                return ErrorCodes.WTFErrorMedicalAttentionSuggested;
            }

            return zone.IsUnitWithCategoryInRange(CategoryFlags.cf_pbs_docking_base, position, (int) typeExclusiveRange)

                ? ErrorCodes.PlacedTooCloseToPBSDockingbase
                : ErrorCodes.NoError;
        }



        public static ErrorCodes CheckZoneForDeployment(IZone zone, Position position, EntityDefault entityDefault)
        {
            List<Position> badSlopes;
            List<Position> badBlocks;

            return CheckZoneForDeployment(zone, position, entityDefault, out badSlopes, out badBlocks);

        }

        public static ErrorCodes CheckZoneForDeployment(IZone zone, Position position, EntityDefault entityDefault,
            out List<Position> badSlopes, out List<Position> badBlocks, bool allCases = false)
        {
            badSlopes = new List<Position>();
            badBlocks = new List<Position>();

            if (entityDefault.Config.constructionRadius == null || entityDefault.Config.blockingradius == null)
            {
                return ErrorCodes.ConsistencyError;
            }

            var contructionRadius = (int) entityDefault.Config.constructionRadius;
            var blockingRadius = (int) entityDefault.Config.blockingradius;

            if (!zone.Configuration.Terraformable)
            {
                return ErrorCodes.ZoneNotTerraformable;
            }

            if (!(entityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_mining_towers) ||
                  entityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_control_tower) ||
                  entityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_energy_well) ||
                  entityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_highway_node)))
            {

                var terrainControlInfo = zone.Terrain.Controls.GetValue(position);

                if (terrainControlInfo.IsAnyTerraformProtected)
                {
                    return ErrorCodes.TileTerraformProtected;
                }

            }


            var centerPosition = position.Center;

            if (zone.IsAnyConstructibleWithinRadius2D(centerPosition, contructionRadius))
            {
                return ErrorCodes.TooCloseToOtherConstructibe;
            }

            var sessionError = ErrorCodes.NoError;

            //slope
            if (!CheckSlopeInRadiusWithBeams(zone, position, blockingRadius, out badSlopes))
            {
                sessionError = ErrorCodes.TerrainTooSteep;
                if (!allCases)
                {
                    return ErrorCodes.TerrainTooSteep;
                }
            }

            //blocking
            if (!IsAnyBlockingInRangeWithBeams(zone, centerPosition, blockingRadius, out badBlocks))
            {
                sessionError = ErrorCodes.BlockedTileWasFoundInConstructionRadius;

                if (!allCases)
                {
                    return ErrorCodes.BlockedTileWasFoundInConstructionRadius;
                }
            }

            if (badBlocks.Any() && badSlopes.Any())
            {
                sessionError = ErrorCodes.TerrainTooSteepAndBlockedTilesWasFound;
            }


            return sessionError;

        }

        /// <summary>
        /// Attempt to push a players from their current locations away from a newly constructed pbs
        /// </summary>
        /// <param name="zone">Zone of pbs and players</param>
        /// <param name="position">Position of pbs deployment</param>
        /// <param name="range">The construction radius of the pbs</param>
        /// <param name="safeMargin">A safe margin to pull inside of the radius</param>
        private static void PushPlayersFromPosition(IZone zone, Position position, int range, double safeMargin = 0.25)
        {
            var epslion = 0.1;
            var safeRadius = range - safeMargin;
            var playersInRange = zone.Players.WithinRange(position, range);

            foreach (var player in playersInRange)
            {
                // Strategy 1: Push the player to the closest tangent to the building radius
                var pushPos = position.GetPositionTowards2D(player.CurrentPosition, Math.Max(safeRadius, epslion));
                // Strategy 2: Check if the position satisfies the constraints, if fail try a new method
                pushPos = TryMovePlayerOutOfRadius(pushPos, position, zone, player, safeRadius);
                Logger.DebugInfo($"player bumped from:{player.CurrentPosition.ToDoubleString2D()} to:{pushPos.ToDoubleString2D()} distance:{pushPos.TotalDistance2D(player.CurrentPosition)}");

                player.TeleportToPositionAsync(pushPos, false, false);
            }
        }

        /// <summary>
        /// Fallback method for PushPlayersFromPosition.
        /// Attempt to find an acceptable location on a radius around a center point to put a player.
        /// </summary>
        /// <param name="edge">Attempted location</param>
        /// <param name="center">Center of circle to move player outside of</param>
        /// <param name="zone">Zone of transform</param>
        /// <param name="player">Player to move</param>
        /// <param name="radius">Radius of circle</param>
        /// <param name="iterations">Number of attempts to try</param>
        /// <param name="epsilon">Epsilon factor to account for rounding in geometric in/equalities</param>
        /// <returns>Modified position</returns>
        private static Position TryMovePlayerOutOfRadius(Position edge, Position center, IZone zone, Player player, double radius, int iterations=100, double epsilon=0.1)
        {
            var r = new Random();
            while ((edge.TotalDistance2D(center) < radius - epsilon || !zone.IsWalkable(edge, player.Slope)) && iterations > 0)
            {
                edge = center.OffsetInDirection(r.NextDouble(), radius);
                iterations--;
            }
            return edge;
        }

        public static void OnPBSEggRemoved(IZone zone, PBSEgg pbsEgg)
        {
            DoTerraformProtectionForEgg(zone, pbsEgg, false);
        }


        public static void OnPBSEggDeployed(IZone zone, PBSEgg pbsEgg)
        {
            DoTerraformProtectionForEgg(zone, pbsEgg, true);
        }

        public static int LazyInitConstructionRadiusByEgg(PBSEgg pbsEgg, ref int constructionRadius)
        {
            if (constructionRadius <= 0)
            {
                var pbsNodeEntityDefault = pbsEgg.TargetPBSNodeDefault;

                var dc = pbsNodeEntityDefault.Config;

                if (dc.constructionRadius == null)
                {
                    Logger.Error("consistency error. no construction radius is defined for:" + pbsNodeEntityDefault +
                                 " " + pbsNodeEntityDefault.Name);
                    constructionRadius = 10;
                }
                else
                {
                    constructionRadius = (int) dc.constructionRadius;
                }
            }

            return constructionRadius;
        }

        public static int LazyInitBlockingRadiusByEgg(PBSEgg pbsEgg, ref int blockingRadius)
        {
            if (blockingRadius <= 0)
            {
                var pbsNodeEntityDefault = pbsEgg.TargetPBSNodeDefault;

                var dc = pbsNodeEntityDefault.Config;

                if (dc.blockingradius == null)
                {
                    Logger.Error("consistency error. no blocking radius is defined for:" + pbsNodeEntityDefault + " " +
                                 pbsNodeEntityDefault.Name);
                    blockingRadius = 10;
                }
                else
                {
                    blockingRadius = (int) dc.blockingradius;
                }
            }

            return blockingRadius;
        }



        private static void DoTerraformProtectionForEgg(IZone zone, PBSEgg pbsEgg, bool state)
        {
            var constructionRadius = pbsEgg.GetConstructionRadius();
            LayerHelper.SetTerrafomProtectionCircle(zone, pbsEgg.CurrentPosition, constructionRadius, state);
        }

        public static void OnPBSObjectDeployed(IZone zone, Unit unit, bool drawTerrainProtection = false,
            bool pushPlayers = false, bool drawEnvironment = false)
        {
            var position = unit.CurrentPosition.Center;

            //eggek meg mittomenmi, nem node
            int constructionRadius;
            if (unit.TryGetConstructionRadius(out constructionRadius))
            {
                if (drawTerrainProtection)
                    LayerHelper.SetTerrafomProtectionCircle(zone, position, constructionRadius, true);

                if (pushPlayers)
                    PushPlayersFromPosition(zone, position, constructionRadius);
            }

            //node only
            var pbsNode = unit as IPBSObject;
            if (pbsNode != null)
            {
                if (drawEnvironment)
                {
                    zone.DrawEnvironmentByUnit(unit);
                    LayerHelper.ClearPlantsCircle(zone, position, constructionRadius);
                    LayerHelper.SetConcreteCircle(zone, position, constructionRadius);
                }
            }
        }

        public static void OnPBSObjectRemovedFromZone<T>(this T removedUnit, IZone zone) where T : Unit, IPBSObject
        {
            var position = removedUnit.CurrentPosition.Center;
            var constructionRadius = removedUnit.GetConstructionRadius();

            LayerHelper.SetTerrafomProtectionCircle(zone, position, constructionRadius, false);
            LayerHelper.ClearConcreteCircle(zone, position, constructionRadius);

            zone.CleanEnvironmentByUnit(removedUnit);

            Logger.DebugInfo("terrain administration done, delete start");
            Logger.DebugInfo("deleting from zone user entities");
            zone.UnitService.RemoveUserUnit(removedUnit);
        }

        private static void PBSFlattenTerrain(IZone zone, Position position, int blockingRadius, int constructionRadius)
        {

            var centerAltitude = zone.Terrain.Altitude.GetValue(position);

            var farRange = constructionRadius;

            using (new TerrainUpdateMonitor(zone))
            {
                for (var j = position.intY - farRange; j <= position.intY + farRange; j++)
                {
                    for (var i = position.intX - farRange; i <= position.intX + farRange; i++)
                    {
                        if (i < 0 || i >= zone.Size.Width || j < 0 || j >= zone.Size.Height) continue;

                        if (position.IsInRangeOf2D(i, j, blockingRadius))
                        {
                            zone.Terrain.Altitude[i, j] = centerAltitude;
                            continue;
                        }

                        double originX = position.intX;
                        double originY = position.intY;
                        var blend = MathHelper.DistanceFalloff(blockingRadius, farRange, originX, originY, i, j);

                        zone.Terrain.Altitude.UpdateValue(i,j, current => current.Mix(centerAltitude, blend));
                    }
                }
            }
        }


        public const double DEGRADE_NEAR_BIAS = 0.4;

        /// <summary>
        /// Degrades a terraformable terrain 
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="position"></param>
        public static void DegradeTowardsOriginal(IZone zone, Position position)
        {
            const int processRadius = 20; // 40x40 rectangle will be processed

            var pbsRangeFar = (int) DistanceConstants.TERRAIN_DEGRADE_DISTANCE_FROM_PBS;
            var pbsRangeNear = (int) (DistanceConstants.TERRAIN_DEGRADE_DISTANCE_FROM_PBS*DEGRADE_NEAR_BIAS);
            var pbsRangeLenght = pbsRangeFar - pbsRangeNear;

            var pbsPositions =
                zone.GetStaticUnits()
                    .Where(o => o is IPBSObject && o.CurrentPosition.TotalDistance2D(position) < pbsRangeFar)
                    .Select(o => o.CurrentPosition)
                    .ToList();

            var altitude = (TerraformableAltitude)zone.Terrain.Altitude;

            using (new TerrainUpdateMonitor(zone))
            {
                for (var j = position.intY - processRadius; j <= position.intY + processRadius; j++)
                {
                    for (var i = position.intX - processRadius; i <= position.intX + processRadius; i++)
                    {
                        if (i < 0 || i >= zone.Size.Width || j < 0 || j >= zone.Size.Height) continue;

                        if (position.IsInRangeOf2D(i, j, processRadius))
                        {
                            //yes the position is in the process circle

                            var originalAltitude = altitude.OriginalAltitude.GetValue(i, j);
                            var currentAltitude = altitude.GetValue(i, j);

                            var dithered = false;
                            if (IsPBSInRange(pbsRangeFar, pbsPositions, i, j))
                            {
                                // yes, pbs in range, calculate falloff
                                dithered = true;

                                var minDistance = GetMinimalDistance(pbsPositions, i, j);

                                var bias = minDistance.LimitWithFalloff(pbsRangeNear, pbsRangeLenght);


                                if (bias <= 0.0) continue;

                                var r = FastRandom.NextDouble();

                                if (r < bias) continue;

                                //bias 0 -> eredeti terep       1 ->  terraformalt, pillanatnyi terep
                                originalAltitude = originalAltitude.Mix(currentAltitude, bias);
                            }

                            if (originalAltitude == currentAltitude) continue;

                            //csak siman kozelitunk az eredeti terep fele

                            var increment = dithered ? 9 : 2;


                            ushort newAltitude;
                            if (originalAltitude > currentAltitude)
                            {
                                newAltitude = (ushort) (currentAltitude + increment).Clamp(0, ushort.MaxValue);
                            }
                            else
                            {
                                newAltitude = (ushort) (currentAltitude - increment).Clamp(0, ushort.MaxValue);
                            }


                            if (dithered)
                            {
                                var currentPosition = new Position(i, j);

                                var sum = (long) newAltitude;

                                foreach (var nPos in currentPosition.GetEightNeighbours(zone.Size))
                                {
                                    var neighbourAltitude = altitude.GetValue(nPos);

                                    sum += neighbourAltitude;
                                }

                                //overwrite the alitude
                                newAltitude = (ushort) (sum/9.0);

                            }

                            zone.Terrain.Altitude[i, j] = newAltitude;
                        }
                    }
                }
            }
        }


        public static bool IsPBSInRange(int range, List<Position> pbsPositions, int x, int y)
        {
            for (var i = 0; i < pbsPositions.Count; i++)
            {
                if (pbsPositions[i].IsInRangeOf2D(x, y, range))
                    return true;
            }

            return false;
        }

        private static Position GetNearestPosition(List<Position> pbsPositions, int x, int y, out double minimumDistance)
        {
            var o = new Position(x, y);

            var nearestPosition = new Position();
            minimumDistance = double.MaxValue;

            foreach (var position in pbsPositions)
            {
                var distance = position.TotalDistance2D(o);

                if (distance < minimumDistance)
                {
                    minimumDistance = distance;
                    nearestPosition = position;
                }

            }

            return nearestPosition;
        }



        public static double GetMinimalDistance(List<Position> pbsPositions, int x, int y)
        {
            var o = new Position(x, y);

            var minimumDistance = double.MaxValue;

            foreach (var position in pbsPositions)
            {
                var distance = position.TotalDistance2D(o);

                if (distance < minimumDistance)
                {
                    minimumDistance = distance;
                }

            }

            return minimumDistance;
        }





        public static IEnumerable<PBSConnection> LoadConnectionsFromSql(IZone zone, long eid)
        {
            if (zone == null)
                yield break;

            var records =
                Db.Query().CommandText("select sourceeid,targeteid,weight,id from pbsconnections where sourceeid=@eid or targeteid=@eid")
                    .SetParameter("@eid", eid)
                    .Execute();

            foreach (var record in records)
            {
                var sourceEid = record.GetValue<long>(0);
                var targetEid = record.GetValue<long>(1);
                var weight = record.GetValue<double>(2);
                var id = record.GetValue<int>(3);

                var isOutGoing = true;
                var getFromZoneTargetEid = targetEid;
                var getFromZoneSourceEid = sourceEid;
                if (targetEid == eid)
                {
                    isOutGoing = false;
                    getFromZoneTargetEid = sourceEid;
                    getFromZoneSourceEid = targetEid;
                }

                var targetUnit = zone.GetUnit(getFromZoneTargetEid);
                if (targetUnit != null)
                {
                    var pbsTargetNode = targetUnit as IPBSObject;
                    if (pbsTargetNode == null)
                        continue;

                    var sourceUnit = zone.GetUnit(getFromZoneSourceEid);
                    if (sourceUnit != null)
                    {
                        var pbsSourceNode = sourceUnit as IPBSObject;
                        if (pbsSourceNode != null)
                        {
                            //connection is valid
                            yield return new PBSConnection(id, pbsTargetNode, pbsSourceNode, isOutGoing, weight);
                        }
                    }
                    else
                    {
                        Logger.Warning("PBS connection load error. SOURCE node not found in zone. eid: " + getFromZoneSourceEid);
                    }
                }
                else
                {
                    Logger.Warning("PBS connection load error. TARGET node not found in zone. eid: " + getFromZoneTargetEid);
                }
            }
        }


        public static void CreatePBSDockingBase(PBSDockingBase dockingBase)
        {
            //parent must be null ---> for structure root and stuff
            dockingBase.Parent = 0;

            var container = PublicContainer.CreateWithRandomEID();
            container.Owner = dockingBase.Owner;
            dockingBase.AddChild(container);

            var market = Market.CreateWithRandomEID();
            market.Owner = dockingBase.Owner;
            dockingBase.AddChild(market);

            var insuraceFacility = Entity.Factory.CreateWithRandomEID(EntityDefault.GetByName(DefinitionNames.PRODUCTION_INSURANCE_FACILITY_BASIC).Definition);
            insuraceFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(insuraceFacility);

            var publicCorporationHangarStorage =
                Entity.Factory.CreateWithRandomEID(DefinitionNames.PUBLIC_CORPORATE_HANGARS_STORAGE);
            publicCorporationHangarStorage.Owner = dockingBase.Owner;
            dockingBase.AddChild(publicCorporationHangarStorage);

            var pbsRefineryFacility =
                Entity.Factory.CreateWithRandomEID(EntityDefault.GetByName(DefinitionNames.PBS_FACILITY_REFINERY).Definition);
            pbsRefineryFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsRefineryFacility);

            var pbsMillFacility = (PBSMillFacility) Mill.CreateWithRandomEID(DefinitionNames.PBS_FACILITY_MILL);
            pbsMillFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsMillFacility);

            var pbsPrototyperFacility = (PBSPrototyperFacility) Prototyper.CreateWithRandomEID(DefinitionNames.PBS_FACILITY_PROTOTYPER);
            pbsPrototyperFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsPrototyperFacility);

            var pbsRepairFacility =
                Entity.Factory.CreateWithRandomEID(EntityDefault.GetByName(DefinitionNames.PBS_FACILITY_REPAIR).Definition);
            pbsRefineryFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsRepairFacility);

            var pbsReprocessorFacility =
                Entity.Factory.CreateWithRandomEID(EntityDefault.GetByName(DefinitionNames.PBS_FACILITY_REPROCESSOR).Definition);
            pbsReprocessorFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsReprocessorFacility);

            var pbsResearchLabFacility = (PBSResearchLabFacility)
                ResearchLab.CreateWithRandomEID(DefinitionNames.PBS_FACILITY_RESEARCH_LAB);
            pbsResearchLabFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsResearchLabFacility);

            var pbsResearchKitForgeFacility =
                Entity.Factory.CreateWithRandomEID(EntityDefault.GetByName(DefinitionNames.PBS_FACILITY_RESEARCH_KIT_FORGE).Definition);
            pbsResearchKitForgeFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsResearchKitForgeFacility);

            var pbsCalibrationProgramForgeFacility = PBSCalibrationProgramForgeFacility.CreateWithRandomEID();
            pbsCalibrationProgramForgeFacility.Owner = dockingBase.Owner;
            dockingBase.AddChild(pbsCalibrationProgramForgeFacility);
        }

        public static void SendPBSDockingBaseCreatedToProduction(long baseEid)
        {
            Task.Run(() =>
            {
                var data = new ProductionRefreshInfo
                {
                    targetPBSBaseEid = baseEid,
                };

                ProductionManager.AddPBSBase(data);
            });
        }

        public static void SendPBSDockingBaseDeleteToProduction(long baseEid)
        {
            Task.Run(() =>
            {
                var data = new ProductionRefreshInfo
                {
                    targetPBSBaseEid = baseEid,
                };

                ProductionManager.RemovePBSBase(data);
            });
        }

        public static int LazyInitBandwidthUsage<T>(T pbsNode, ref int bandwidthUsage) where T : Unit, IPBSObject
        {
            if (bandwidthUsage < 0)
            {
                var dc = pbsNode.ED.Config;
                if (dc.bandwidthUsage == null)
                {
                    Logger.Error("consistency error. no bandwith usage was defined for definition: " +
                                 pbsNode.Definition + " " + pbsNode.ED.Name);
                    bandwidthUsage = 0;
                }
                else
                {
                    bandwidthUsage = (int) dc.bandwidthUsage;
                }
            }

            return bandwidthUsage;
        }






        public static int LazyReinforceCounterMax<T>(T pbsNode, ref int reinforceCounterMax) where T : Unit
        {
            if (reinforceCounterMax <= 0)
            {
                var dc = pbsNode.ED.Config;
                if (dc.reinforceCounterMax == null)
                {
                    reinforceCounterMax = 1;
                }
                else
                {
                    reinforceCounterMax = (int) dc.reinforceCounterMax;
                }
            }
            return reinforceCounterMax;
        }


        public static int LazyInitConstrustionLevelMax<T>(T constructible, ref int constructionLevelMax) where T : Unit, IPBSObject
        {
            if (constructionLevelMax <= 0)
            {
                var dc = constructible.ED.Config;
                if (dc.constructionlevelmax == null)
                {
                    Logger.Error("consistency error. no constructionLevelMax was defined for definition: " +
                                 constructible.Definition + " " + constructible.ED.Name);
                    constructionLevelMax = 100;
                }
                else
                {
                    constructionLevelMax = (int) dc.constructionlevelmax;
                }
            }
            return constructionLevelMax;
        }

        public static int LazyInitChargeAmount<T>(T pbsObject, ref int chargeAmount) where T : Unit, IPBSObject
        {
            if (chargeAmount <= 0)
            {
                var dc = pbsObject.ED.Config;
                if (dc.chargeAmount == null)
                {
                    Logger.Error("consistency error. no chargeAmount was defined for definition: " +
                                 pbsObject.Definition + " " + pbsObject.ED.Name);
                    chargeAmount = 100;
                }
                else
                {
                    chargeAmount = (int) dc.chargeAmount;
                }
            }
            return chargeAmount;
        }

        public static int LazyInitProductionLevelIncrease<T>(T pbsNode, ref int upgradeAmount) where T : Unit, IPBSObject
        {
            if (upgradeAmount <= 0)
            {
                var dc = pbsNode.ED.Config;
                if (dc.productionUpgradeAmount == null)
                {
                    Logger.Error("consistency error. no productionUpgradeAmount was defined for definition: " +
                                 pbsNode.Definition + " " + pbsNode.ED.Name);
                    upgradeAmount = 10;
                }
                else
                {
                    upgradeAmount = (int) dc.productionUpgradeAmount;
                }
            }
            return upgradeAmount;
        }


        public static int LazyInitProductionLevelBase<T>(T pbsNode, ref int productionLevel) where T : Unit, IPBSObject
        {
            if (productionLevel <= 0)
            {
                var dc = pbsNode.ED.Config;
                if (dc.productionLevel == null)
                {
                    Logger.Error("consistency error. no productionLevel was defined for definition: " +
                                 pbsNode.Definition + " " + pbsNode.ED.Name);
                    productionLevel = 10;
                }
                else
                {
                    productionLevel = (int) dc.productionLevel;
                }
            }
            return productionLevel;
        }


        public static void DropLootToZone<T>(IZone zone, T pbsNode, Unit killer) where T : Unit, IPBSObject
        {
            Db.CreateTransactionAsync(scope =>
            {
                HandleNodeDead(zone, pbsNode, killer).ThrowIfError();
            });
        }

        private static ErrorCodes HandleNodeDead<T>(IZone zone, T pbsNode, Unit killer) where T : Unit, IPBSObject
        {
            var ec = ErrorCodes.NoError;

            //itt csinalunk mindenfelet, ami csak tetszik

            int? killerCharacterId = null;

            if (killer is Player killerPlayer)
            {
                killerCharacterId = killerPlayer.Character.Id;
            }

            var lootPosition = pbsNode.CurrentPosition.GetRandomPositionInRange2D(0, pbsNode.GetConstructionRadius());
            //ha minden oke akkor majd dobjuk a lootot
            LootContainer.Create()
                                .AddLoot(GetLootFromCapsule(pbsNode))
                                .AddLoot(GetConstructionAmmoLootOnDead(pbsNode))
                                .BuildAndAddToZone(zone, lootPosition);

            Transaction.Current.OnCommited(() =>
            {
                WritePBSLog(PBSLogType.killed, pbsNode.Eid, pbsNode.Definition, pbsNode.Owner, background: false,
                    zoneId: zone.Id, killerCharacterId: killerCharacterId);
            });

            return ec;
        }

        public const int CONSTRUCTION_AMMO_DEFINITION = 4658; //TODO: Fetch this from DB

        public static IEnumerable<LootItem> GetConstructionAmmoLootOnDead(IPBSObject pbsObject)
        {
            var constructionLevelMax = pbsObject.ConstructionLevelMax;

            var amount = (int)(constructionLevelMax*0.7*pbsObject.ConstructionLevelCurrent/constructionLevelMax);

            var constructionLootList = new List<LootItem>();

            if (amount > 0)
            {
                constructionLootList.Add(LootItemBuilder.Create(CONSTRUCTION_AMMO_DEFINITION).SetQuantity(amount).Build());
            }

            return constructionLootList;
        }

        public static IEnumerable<LootItem> GetConstructionAmmoLootOnDeconstruct(IPBSObject pbsObject)
        {
            var amount = (int) (pbsObject.ConstructionLevelMax*0.7);

            var constructionLootList = new List<LootItem>();

            if (amount > 0)
            {
                constructionLootList.Add(LootItemBuilder.Create(CONSTRUCTION_AMMO_DEFINITION).SetQuantity(amount).Build());
            }

            return constructionLootList;
        }

        public static void DropLootToZoneFromBase(IZone zone, PBSDockingBase pbsDockingBase, Unit killer)
        {
            Logger.DebugInfo("async hand dead started");
            if (pbsDockingBase == null)
                return;

            Logger.DebugInfo("valid async target");
            Db.CreateTransactionAsync(scope =>
            {
                HandleDockingBaseDead(zone, pbsDockingBase, killer).ThrowIfError();
            });
        }

        public static ErrorCodes HandleDockingBaseDead(IZone zone, PBSDockingBase pbsDockingBase, Unit killer)
        {
            var ec = ErrorCodes.NoError;

            Logger.DebugInfo(" ########    docking base SQL DROP LOOT Start ");

            try
            {
                //itt csinalunk mindenfelet, ami csak tetszik
                //sql transactionban vagyunk

                //elokeszitjuk a lootot
                var lootPosition = pbsDockingBase.CurrentPosition.GetRandomPositionInRange2D(0,
                    pbsDockingBase.GetConstructionRadius());

                LootContainer.Create()
                                    .AddLoot(GetLootFromCapsule(pbsDockingBase))
                                    .AddLoot(GetConstructionAmmoLootOnDead(pbsDockingBase))
                                    .BuildAndAddToZone(zone, lootPosition);

                //eddig jo innen 0sszeszedjuk sqlbol a cuccot ami van a bazison, hozzadjuk a loot hoz, es kesz
                //getfulltree + filter by type
                DropLootToZoneFromBaseItems(zone, pbsDockingBase);

                int? killerCharacterId = null;

                if (killer is Player killerPlayer)
                {
                    killerCharacterId = killerPlayer.Character.Id;
                }

                WritePBSLog(PBSLogType.killed, pbsDockingBase.Eid, pbsDockingBase.Definition, pbsDockingBase.Owner,
                    background: false, zoneId: zone.Id, killerCharacterId: killerCharacterId);

                Logger.DebugInfo(" ########    docking base SQL DROP LOOT STOP ");

            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                ec = ErrorCodes.ServerError;
            }
            finally
            {
                pbsDockingBase.IsLootGenerating = false;
            }
            return ec;
        }

        public static void DropLootToZoneFromBaseItems(IZone zone, PBSDockingBase pbsDockingBase,bool damaged = true)
        {
            IEnumerable<LootItem> allEntities;

            if (damaged)
            {
                allEntities = GetDamagedLootFromBase(pbsDockingBase.Eid).ToList();
            }
            else
            {
                allEntities = GetFullLootFromBase(pbsDockingBase.Eid).ToList();
            }

            foreach (var oneSlice in allEntities.Slice(1000))
            {
                Logger.DebugInfo("slice dropping");
                var lootPosition = pbsDockingBase.CurrentPosition.GetRandomPositionInRange2D(0,
                    pbsDockingBase.GetConstructionRadius());

                LootContainer.Create().AddLoot(oneSlice).BuildAndAddToZone(zone, lootPosition);
            }
        }

        public static IEnumerable<LootItem> GetFullLootFromBase(long baseEid)
        {
            Logger.Info("getting loot from " + baseEid);

            TransportAssignment.DockingBaseKilled(baseEid);

            var thisBase = (DockingBase) Entity.Repository.LoadRawTree(baseEid);

            if (thisBase == null)
            {
                Logger.Error("mi is van itt? mer van torolve? ");
                return new List<LootItem>();
            }

            LogDockingBaseLoot(thisBase);


            var lootList = new List<LootItem>();
            foreach (var entity in thisBase.GetFullTree())
            {
                if (!entity.ED.Purchasable)
                    continue;

                //safety
                if (entity.Quantity <= 0)
                    continue;

                var builder = LootItemBuilder.Create(entity.Definition);

                if (entity.IsCategory(CategoryFlags.cf_material) ||
                    entity.IsCategory(CategoryFlags.cf_dogtags) ||
                    entity.IsCategory(CategoryFlags.cf_kernels) ||
                    entity.IsCategory(CategoryFlags.cf_production_items) ||
                    entity.IsCategory(CategoryFlags.cf_field_accessories) ||
                    entity.IsCategory(CategoryFlags.cf_pbs_capsules) ||
                    entity.IsCategory(CategoryFlags.cf_ammo))
                {

                    builder.SetQuantity(entity.Quantity);
                }
                else if (entity.IsCategory(CategoryFlags.cf_robots))
                {

                    builder.SetQuantity(entity.Quantity).AsRepackaged();
                }
                else if (entity.IsCategory(CategoryFlags.cf_robot_equipment))
                {

                    builder.SetQuantity(entity.Quantity).AsRepackaged();
                }

                if (entity is VolumeWrapperContainer container)
                {
                    //get the content as loot and delete immediately
                    container.SetAllowDelete();
                    lootList.AddRange(container.GetLootItems());
                    Entity.Repository.Delete(container);
                }

                lootList.Add(builder.Build());
            }

            return lootList;
        }

        private static void LogDockingBaseLoot(Entity dockingBase)
        {
            foreach (var entity in dockingBase.GetFullTree())
            {
                Logger.Info($"PBSLOOT {entity.GetType()} eid:{entity.Eid} {entity.ED.Name} q:{entity.Quantity}");
            }
        }

        private static IEnumerable<LootItem> GetDamagedLootFromBase(long baseEid)
        {
            Logger.Info("getting loot from " + baseEid);

            //--transport assignments
            TransportAssignment.DockingBaseKilled(baseEid);

            var thisBase = (PBSDockingBase) Entity.Repository.LoadRawTree(baseEid);
            if (thisBase == null)
            {
                Logger.Error("mi is van itt? mer van torolve? ");
                return new List<LootItem>();
            }

            LogDockingBaseLoot(thisBase);

            var lootList = new List<LootItem>();
            foreach (var entity in thisBase.GetFullTree())
            {
                if (!entity.ED.Purchasable)
                    continue;

                var builder = LootItemBuilder.Create(entity.Definition);

                if (entity.IsCategory(CategoryFlags.cf_material) ||
                    entity.IsCategory(CategoryFlags.cf_dogtags) ||
                    entity.IsCategory(CategoryFlags.cf_production_items) ||
                    entity.IsCategory(CategoryFlags.cf_field_accessories) ||
                    entity.IsCategory(CategoryFlags.cf_pbs_capsules) ||
                    entity.IsCategory(CategoryFlags.cf_ammo))
                {
                    var quantity = FastRandom.NextInt(0, entity.Quantity);
                    if (quantity == 0)
                        continue;

                    builder.SetQuantity(quantity);
                }
                else if (entity.IsCategory(CategoryFlags.cf_robots))
                {
                    //itt csak darabra tudunk randomozni

                    if (!entity.IsRepackaged)
                    {
                        //50% hogy nem esik
                        if (FastRandom.NextDouble() < 0.5)
                            continue;

                        //nincs becsomagolva
                        builder.SetQuantity(1);
                    }
                    else
                    {
                        //be van csomagolva
                        var quantity = FastRandom.NextInt(0, entity.Quantity);
                        if (quantity == 0)
                            continue;

                        builder.SetQuantity(quantity);
                    }

                    builder.AsRepackaged();
                }
                else if (entity.IsCategory(CategoryFlags.cf_robot_equipment))
                {
                    if (entity.IsRepackaged)
                    {
                        //darabra 

                        var quantity = FastRandom.NextInt(0, entity.Quantity);
                        if (quantity == 0)
                            continue;

                        builder.SetQuantity(quantity).AsRepackaged();
                    }
                    else
                    {
                        //damageljuk
                        builder.SetQuantity(1).AsDamaged();
                    }
                }

                if (entity is VolumeWrapperContainer)
                {
                    //get the content as loot and delete immediately
                    var c = entity as VolumeWrapperContainer;
                    c.SetAllowDelete();
                    lootList.AddRange(c.GetLootItems());
                    Entity.Repository.Delete(c);
                }

                if (LootHelper.Roll(0.8))
                    lootList.Add(builder.Build());
            }

            return lootList;
        }


        public static List<LootItem> GetLootFromCapsule(IPBSObject pbsNode)
        {
            var capsuleDefinition = GetCapsuleDefinitionByPBSObject(pbsNode);

            var productionComponents = ProductionDataAccess.ProductionComponents[capsuleDefinition];

            var lootItems = new List<LootItem>();

            foreach (var productionComponent in productionComponents)
            {
                if (!productionComponent.IsMaterial)
                    continue;

                var amount = (int) (productionComponent.Amount*FastRandom.NextDouble(0.3, 0.7));
                if (amount <= 0)
                    continue;

                var lootItem = LootItemBuilder.Create(productionComponent.EntityDefault.Definition).SetQuantity(amount).Build();
                lootItems.Add(lootItem);
            }

            return lootItems;
        }

        private static void WaitForLootGenerator(PBSDockingBase dockingBase)
        {
            //ezzel varjuk meg amig a loot droppolo thread vegez, sokaig is tarthat
            var counter = 0;
            while (dockingBase.IsLootGenerating)
            {
                Thread.Sleep(100);
                Logger.DebugInfo("loot is being generated for base " + dockingBase.Name + " " + dockingBase.Eid);
                if (counter++ > 9000)
                {
                    Logger.Error("loot generator thread got stuck for base: " + dockingBase.Name + " " + dockingBase.Eid);
                    break; //lets dance
                }
            }
        }


        public static ErrorCodes DeletePBSDockingBase(int zone, PBSDockingBase dockingBase)
        {
            ErrorCodes ec;

            WaitForLootGenerator(dockingBase);

            using (var scope = Db.CreateTransaction())
            {
                ec = dockingBase.DoCleanUpWork(zone);

                if (ec != ErrorCodes.NoError)
                {
                    Logger.Error($"{ec} occured during pbs docking base delete, transaction closed anyway. {dockingBase}");
                }

                scope.Complete();
            }
            
            return ec;
        }

        public static void SendBaseDestroyed(Dictionary<string, object> infoBaseDeadWhileDocked, Dictionary<string, object> infoBaseDeadWhileOnZone, IEnumerable<Tuple<Character, long, bool>> charactersDocked)
        {
            //itt mindenkinek egyesevel elmondjuk hogy hova lett visszateve
            foreach (var tuple in charactersDocked)
            {
                //mindenkinek mas lehet, ahova pakolva lettek
                var character = tuple.Item1;
                var dockingBaseEid = tuple.Item2;
                var wasOnZone = !tuple.Item3; //!docked

                Dictionary<string, object> characterDict;
                if (wasOnZone)
                {
                    characterDict = infoBaseDeadWhileOnZone.Clone();
                    characterDict.Add(k.baseEID, dockingBaseEid);
                }
                else
                {
                    characterDict = infoBaseDeadWhileDocked.Clone();
                    characterDict.Add(k.baseEID, dockingBaseEid);
                }

                Message.Builder.SetCommand(Commands.PbsEvent).WithData(characterDict).ToCharacter(character).Send();
            }
        }


        public static Dictionary<string, object> GetUpdateDictionary(int zone,Unit eventSource,PBSEventType pbsEventType, Dictionary<string, object> data = null)
        {
            Logger.Warning($"Zone {zone} eventSource {eventSource} event type {pbsEventType}");
            var sourceDict = eventSource.ToDictionary();

            if (data == null)
                data = new Dictionary<string, object>();
#if DEBUG
            data.Add(k.reason, pbsEventType.ToString()); //ez csak debug !!! hogy olvashato legyen
#endif
            data.Add(k.message, (int)pbsEventType);
            data.Add(k.source, sourceDict);
            data[k.zoneID] = zone; //OPP: client does not accept null or invalid zoneIDs!
            return data;
        }
        
        public static int CollectFacilityNodeLevelFromInComingConnections(PBSProductionFacilityNode pbsProductionFacilityNode)
        {
            var inConnections = pbsProductionFacilityNode.ConnectionHandler.InConnections;

            //my default base value
            var levelBase = pbsProductionFacilityNode.GetFacilityLevelBase();

            //collect connection value
            foreach (var pbsConnection in inConnections)
            {
                if (!(pbsConnection.TargetPbsObject is PBSFacilityUpgradeNode node))
                    continue;
                //igen upgrade node

                if (node.IsContributing())
                {
                    levelBase += node.GetLevelIncrease();
                }
            }

            return levelBase;
        }


        public static ErrorCodes UpdateWeightToSql(PBSConnection connection)
        {
            if (connection != null)
            {
                var res = Db.Query().CommandText("update pbsconnections set weight=@weight where id=@id")
                                 .SetParameter("@id", connection.Id)
                                 .SetParameter("@weight", connection.Weight)
                                 .ExecuteNonQuery();

                return res == 1 ? ErrorCodes.NoError : ErrorCodes.SQLUpdateError;
            }

            return ErrorCodes.NoError;
        }


        private const double  MAXIMUM_PBS_SLOPE = 2.75;

        public static bool CheckSlopeInRadiusWithBeams(IZone zone, Position origin, int radius, out List<Position> badSlopes)
        {
            CollectConditions(zone, origin, radius, out badSlopes, (x, y) => zone.Terrain.Slope.CheckSlope(x, y, MAXIMUM_PBS_SLOPE));

            if (badSlopes.Count > 0)
            {
                zone.CreateSingleBeamToPositions(BeamType.green_10sec,1337,badSlopes);
            }

            return badSlopes.Count == 0; //akkor jo ha nincs egy rossz pozicio sem

        }

        public static bool IsAnyBlockingInRangeWithBeams(IZone zone, Position origin, int radius, out List<Position> badBlocks)
        {
            CollectConditions(zone, origin, radius, out badBlocks, (x, y) => zone.Terrain.Blocks.GetValue(x, y).Flags == BlockingFlags.Undefined);

            if (badBlocks.Count > 0)
            {
                zone.CreateSingleBeamToPositions(BeamType.green_10sec,1337,badBlocks);
            }

            return badBlocks.Count== 0;
        }
     
        public static void CollectConditions(IZone zone, Position origin, int radius, out List<Position> troubledPositions, Func<int, int, bool> conditionTest )
        {
            origin = origin.Center;
            troubledPositions = new List<Position>();
            var width = zone.Size.Width;
            var height = zone.Size.Height;
            var area = Area.FromRadius(origin, radius);

            for (var j = area.Y1; j <= area.Y2; j++)
            {
                for (var i = area.X1; i <= area.X2; i++)
                {
                    if (i < 0 || i >= width || j < 0 || j >= height) continue;

                    //keep the circle shape if the radius
                    if (radius > 1)
                    {
                        if (!origin.IsWithinOrEqualRange(i + 0.5, j + 0.5, radius)) continue;
                    }

                    if (!conditionTest(i,j))
                    {
                        troubledPositions.Add(new Position(i, j).Center);
                    }
                }
            }
        }


        public static void WritePBSLog(PBSLogType logType, long nodeEid, int nodeDefinition, long corporationEid, 
            int? issuerCharacterId = null,
            long? takeOverCorporationEid = null,
            long? otherNodeEid = null,
            int? materialDefinition = null,
            int? materialAmount = null,
            int? otherNodeDefinition = null,
            int? zoneId = null,
            int? killerCharacterId = null,
            bool background = true
            )
        {
            if (background)
            {
                Task.Run(() => InsertPBSLog(logType, nodeEid, nodeDefinition, corporationEid, issuerCharacterId, takeOverCorporationEid, otherNodeEid, materialDefinition, materialAmount,otherNodeDefinition,zoneId, killerCharacterId)).LogExceptions();
            }
            else
            {
                InsertPBSLog(logType, nodeEid, nodeDefinition, corporationEid, issuerCharacterId, takeOverCorporationEid, otherNodeEid, materialDefinition, materialAmount, otherNodeDefinition,zoneId, killerCharacterId);
            }

        }


        private static void InsertPBSLog(PBSLogType logType, long nodeEid, int nodeDefinition, long corporationEid, 
            int? issuerCharacterId = null,
            long? takeOverCorporationEid = null,
            long? otherNodeEid = null,
            int? materialDefinition = null,
            int? materialAmount = null,
            int? otherNodeDefinition = null,
            int? zoneId = null,
            int? killerCharacterId = null
            )
        {
            var res = DynamicSqlQuery.Insert("pbslog", new{ 
                eventtype=  logType, 
                nodeeid=  nodeEid, 
                nodedefinition= nodeDefinition,
                corporationeid= corporationEid,
                issuercharacterid = issuerCharacterId, 
                takeovercorporationeid = takeOverCorporationEid,
                othernodeeid= otherNodeEid, 
                materialdefinition =materialDefinition,
                materialamount =materialAmount,
                othernodedefinition = otherNodeDefinition,
                zoneid = zoneId,
                killercharacterid = killerCharacterId,
            });

        }



        public static IDictionary<string, object> GetPBSLog(int offsetInDays, long corporationEid, int zoneId)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-2);

            const string sqlCmd = @"SELECT 

nodeeid,
nodedefinition,
eventtime,
eventtype,
issuercharacterid,
takeovercorporationeid,
othernodeeid,
othernodedefinition,
materialdefinition,
materialamount,
zoneid,
killercharacterid

                                    FROM pbslog 
                                    WHERE corporationeid = @corporationEid AND (eventtime between @earlier AND @later) and zoneid=@zoneId";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@corporationEid",corporationEid)
                .SetParameter("@earlier",earlier)
                .SetParameter("@later",later)
                .SetParameter("@zoneId",zoneId)
                .Execute().RecordsToDictionary("p");
            return result;
        }

        /// <summary>
        /// Traverse to node definition from capsule definition
        /// </summary>
        /// <param name="capsuleDefinition"></param>
        /// <returns></returns>
        public static int GetNodeDefinitionByCapsule(int capsuleDefinition)
        {
            var dc = EntityDefault.Get(capsuleDefinition).Config;

            if (dc.targetDefinition == null)
            {
                Logger.Error("consistency error in " + EntityDefault.Get(capsuleDefinition).Name);
                return 0;
            }

            var eggDefinition = (int)dc.targetDefinition;

            var eggDc = EntityDefault.Get(eggDefinition).Config;
            if (eggDc.targetDefinition == null)
            {
                Logger.Error("consistency error in " + EntityDefault.Get(eggDefinition).Name);
                return 0;
            }

            return (int) eggDc.targetDefinition;
        }

        public static ErrorCodes ValidatePBSNodePlacing(IZone zone, Position spawnPosition, long owner, EntityDefault pbsNodeEntityDefault)
        {
            var ourBases = zone.Units.OfType<PBSDockingBase>().Where(b=>b.Owner == owner).ToList();

            var isInRange = ourBases.Any(b =>{
                    var dockingBaseRange = b.GetNetworkNodeRange();
                    return spawnPosition.IsInRangeOf2D(b.CurrentPosition, dockingBaseRange);
                });
            
            return isInRange ? ErrorCodes.NoError  : ErrorCodes.NoOwnedBaseInRange;
        }

        public static void FeedWithItems<T>(T pbsObject, Player player, IEnumerable<long> eids) where T:Unit,IPBSObject
        {
            //lehet offline reactort etetni? - most lehet
            pbsObject.IsFullyConstructed().ThrowIfFalse(ErrorCodes.ObjectNotFullyConstructed);
            pbsObject.CoreMax.ThrowIfLessOrEqual( pbsObject.Core, ErrorCodes.ReactorIsFull);

            var logDict = new Dictionary<int, int>();

            var container = player.GetContainer();
            Debug.Assert(container != null, "container != null");
            container.EnlistTransaction();

            var finalCore = 0.0;
            foreach (var eid in eids)
            {
                var item = container.GetItem(eid);
                if (item == null)
                    continue;

                var coreValue = item.ED.Config.CoreCalories;
                if (coreValue <= 0.0)
                    continue;

                var bufferSpace = pbsObject.CoreMax - pbsObject.Core;

                var quantityLeft = (int)Math.Ceiling(bufferSpace / coreValue);

                var takenQuantity = Math.Min(item.Quantity, quantityLeft);

                if (takenQuantity <= 0)
                    continue;

                if (takenQuantity == item.Quantity)
                {
                    //delete
                    Entity.Repository.Delete(item);
                }
                else
                {
                    //update
                    item.Quantity = item.Quantity - takenQuantity;
                }

                finalCore += takenQuantity * coreValue;

                if (logDict.ContainsKey(item.Definition))
                {
                    logDict[item.Definition] += takenQuantity;
                }
                else
                {
                    logDict[item.Definition] = takenQuantity;
                }
            }

            container.Save();

            if (finalCore <= 0.0)
                return;

            pbsObject.Core += finalCore;

            foreach (var pair in logDict)
            {
                WritePBSLog(PBSLogType.materialSubmitted, pbsObject.Eid, pbsObject.Definition, pbsObject.Owner,
                player.Character.Id,
                background: false,
                zoneId: pbsObject.Zone.Id,
                materialDefinition: pair.Key,
                materialAmount: pair.Value);
            }
        }

        public static void CheckWallPlantingAndThrow(IZone zone, Unit[] unitsInZone, Position targetPosition, long corporationEid)
        {
            zone.Configuration.Protected.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);

            if (zone.Configuration.IsBeta)
            {
                //only around owned outpost

                //BETA RULE
                var myOutPostAround = unitsInZone.OfType<Outpost>().WithinRange(targetPosition, DistanceConstants.PLANT_MAX_DISTANCE_FROM_OUTPOST)
                    .Any(o => o.GetIntrusionSiteInfo().Owner == corporationEid);

                myOutPostAround.ThrowIfFalse(ErrorCodes.OnlyPossibleAroundOwnedOutposts);
            }

            if (zone.Configuration.IsGamma)
            {
                //GAMMA RULE
                var terrainControlInfo = zone.Terrain.Controls.GetValue(targetPosition);

                //nem lehet terraform protected biten
                terrainControlInfo.IsAnyTerraformProtected.ThrowIfTrue(ErrorCodes.TileTerraformProtected);

                //ennel k0zelebb nem lehet semmijen pbshez
                unitsInZone.WithinRange(targetPosition, DistanceConstants.PLANT_MIN_DISTANCE_FROM_PBS)
                    .Any(u => u is IPBSObject).ThrowIfTrue(ErrorCodes.PlantingNotAllowedCloseToStructures);

                //csak sajat docking base es control tower korul lehet egy adott range-ig
                unitsInZone.WithinRange(targetPosition, DistanceConstants.PLANT_MAX_DISTANCE_FROM_PBS)
                    .Any(u =>
                    {
                        if (u is PBSDockingBase || u is PBSControlTower)
                        {
                            if (u.Owner == corporationEid)
                                return true;
                        }

                        return false;
                    }
                    ).ThrowIfFalse(ErrorCodes.OnlyPossibleAroundOwnenStructures);
            }
        }
    }
}
