using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones.PlantTools
{
    /// <summary>
    /// Deploys a plant form a seed item from a robot inventory
    /// </summary>
    public class PlantSeedDeployer : ItemDeployer
    {
        public PlantSeedDeployer(IEntityServices entityServices) : base(entityServices)
        {
        }

        private PlantType GetTargetPlantType()
        {
            var plantIndex = (byte)ED.Options.Type;

            if (plantIndex == 0)
            {
                Debug.Assert(false, "no plant type defined for " + ED.Definition + " " + ED.Name);
                return PlantType.NotDefined;
            }

           if (Enum.IsDefined(typeof(PlantType), plantIndex))
           {
               return (PlantType) plantIndex;
           }

            Debug.Assert(false, "plant seed type not defined in PlantType enum for definition: " + ED.Definition + " " + ED.Name + " option key: " + plantIndex);
           return PlantType.NotDefined;

        }

        private Position _targetPosition;
        private Unit[] _currentUnits;
        private long _corporationEid;
        private PlantType _targetPlantType;
        private PlantRule _plantRule;


        /// <summary>
        /// Checks variuos zone and plantrule conditions to plant 
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="player"></param>
        public override void Deploy(IZone zone, Player player)
        {
            //plant it under the player
            _targetPosition = player.CurrentPosition;
            
            //this item will plant this plant
            _targetPlantType = GetTargetPlantType();

            _plantRule = zone.Configuration.PlantRules.GetPlantRule(_targetPlantType);
            if (_plantRule == null)
            {
                //the desired plantrule was not found on zone

#if DEBUG
                Logger.Error("consistency error. no plantrule found for seed:" + Definition + " planttype:" + _targetPlantType);
#endif
                //different errormessage for concrete placing
                _targetPlantType.ThrowIfEqual(PlantType.Devrinol,ErrorCodes.ZoneNotTerraformable);
                
                throw new PerpetuumException(ErrorCodes.PlantNotFertileOnThisZone);
            }

            //copy units to local for many iterations
            _currentUnits = zone.Units.ToArray();

            //save the players corporationeid to check beta and gamma conditions
            _corporationEid = player.CorporationEid;


            if (!_plantRule.PlacesConcrete)
            {
                CheckNonConcreteAndThrow(zone);
            }

            //these plants can't be placed on alpha
            if (zone.Configuration.IsAlpha)
            {
                (_targetPlantType == PlantType.Devrinol).ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);

            }
            else
            {
                //on beta and gamma we need corporationeid to match with stuff
                DefaultCorporationDataCache.IsCorporationDefault(_corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);
            }

            if (_targetPlantType == PlantType.Wall)
            {
                PBSHelper.CheckWallPlantingAndThrow(zone, _currentUnits, _targetPosition, _corporationEid);
            }


            if (_plantRule.PlacesConcrete)
            {
                zone.Configuration.Terraformable.ThrowIfFalse(ErrorCodes.ZoneNotTerraformable);
                PlaceConcreteOrThrow(zone);
            }
            else
            {
                PutPlantOrThrow(zone,_targetPosition.intX,_targetPosition.intY);
            }

            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.ItemDeploy).SetCharacter(player.Character).SetItem(Definition, 1);
            player.Character.LogTransaction(b);

        }

        private void PutPlantOrThrow(IZone zone,int x, int y)
        {
            IsPositionValidForPlantingOrThrow(zone, x, y, _targetPlantType);

            if (_targetPlantType == PlantType.NotDefined) 
                return;

            using (new TerrainUpdateMonitor(zone))
            {
                zone.Terrain.PutPlant(x, y, 0, _targetPlantType, _plantRule);
            }

            zone.CreateBeam(BeamType.seeddeploy, builder => builder.WithPosition(new Position(x + 0.5, y + 0.5)).WithDuration(5000));
        }

        private void PlaceConcreteOrThrow(IZone zone)
        {

            var posList = new List<KeyValuePair<int, int>>();

            var iX = _targetPosition.intX;
            var iY = _targetPosition.intY;

            for (var j = iY - 2; j <= iY + 2; j++)
            {
                for (var i = iX - 2; i <= iX + 2; i++)
                {
                    posList.Add(new KeyValuePair<int, int>(i, j));
                }
            }

            var counter = 0;
            while (posList.Count > 0)
            {
                var index = FastRandom.NextInt(0, posList.Count - 1);

                var pair = posList.ElementAt(index);
                posList.RemoveAt(index);

                var i = pair.Key;
                var j = pair.Value;

                if (i < 0 || i >= zone.Size.Width || j < 0 || j >= zone.Size.Height)
                    continue;

                try
                {
                    PutPlantOrThrow(zone,i, j);
                    counter++;
                }
                catch (Exception ex)
                {
                    if (ex is PerpetuumException)
                        continue;
                    throw;
                }

                if (FastRandom.NextDouble() < 0.5)
                {
                    Thread.Sleep(FastRandom.NextInt(0, 150));
                }
            }

            counter.ThrowIfZero(ErrorCodes.NoConcreteWasPlaced);
        }


      
        /// <summary>
        /// every type of plant except for concrete placing
        /// </summary>
        private void CheckNonConcreteAndThrow(IZone zone)
        {
            //distance from public docking bases and teleports
            _currentUnits
                .Any(u => ((u is DockingBase && !(u is PBSDockingBase)) || u is Teleport) && u.IsInRangeOf3D(_targetPosition, DistanceConstants.PLANT_MIN_DISTANCE_FROM_BASE)).ThrowIfTrue(ErrorCodes.PlantingNotAllowedNearBases);

            //on beta check SAP positions
            if (zone.Configuration.IsBeta)
            {
                //BETA general planting
                var sapinfos = _currentUnits.OfType<Outpost>().SelectMany(o => o.SAPInfos);

                foreach (var sapInfo in sapinfos)
                {
                    sapInfo.Position
                        .IsInRangeOf2D(_targetPosition, DistanceConstants.PLANT_MIN_DISTANCE_FROM_SAP).ThrowIfTrue(ErrorCodes.SAPIsInRange);
                }
            }
            
        }


        /// <summary>
        /// Checks if the position is valid for the given PlantType
        /// </summary>
        private void IsPositionValidForPlantingOrThrow(IZone zone,int x, int y, PlantType targetPlant)
        {
            var terrain = zone.Terrain;

            var targetPlantRule = zone.Configuration.PlantRules.GetPlantRule(targetPlant).ThrowIfNull(ErrorCodes.PlantNotFertileOnThisZone);

            var plantInfo = terrain.Plants.GetValue(x, y);

            var terrainSlope = terrain.Slope.GetValue(x, y);
            (terrainSlope > targetPlantRule.Slope || terrainSlope < targetPlantRule.MinSlope).ThrowIfTrue(ErrorCodes.TerrainTooSteep);

            if (targetPlantRule.OnlyOnUnprotectedZone)
            {
                zone.Configuration.Protected.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);
            }

            if (!targetPlantRule.AllowedOnNonNatural)
            {
                targetPlantRule.AllowedTerrainTypes.Contains(plantInfo.groundType).ThrowIfFalse(ErrorCodes.InvalidConditionsForPlanting);

                var controlInfo = zone.Terrain.Controls.GetValue(x, y);

                controlInfo.IsAnyHighway.ThrowIfTrue(ErrorCodes.PlantingNotAllowedOnHighway);
                controlInfo.AnyConcrete.ThrowIfTrue(ErrorCodes.PlantingNotAllowedOnConcrete);

                controlInfo.AntiPlant.ThrowIfTrue(ErrorCodes.ServerControlledPlanting);
                controlInfo.SyndicateArea.ThrowIfTrue(ErrorCodes.ServerControlledPlanting);


                //(controlInfo.AntiPlant || controlInfo.IsAnyHighway || controlInfo.AnyConcrete || controlInfo.SyndicateArea).ThrowIfTrue(ErrorCodes.InvalidConditionsForPlanting);

                var altitudeValue = zone.Terrain.Altitude.GetAltitude(x, y) / 4;
                (targetPlantRule.AllowedAltitudeLow > altitudeValue || targetPlantRule.AllowedAltitudeHigh < altitudeValue).ThrowIfTrue(ErrorCodes.InvalidConditionsForPlanting);

                var waterLevel = ZoneConfiguration.WaterLevel / 4;
                var altRelativeToWater = altitudeValue - waterLevel;

                (targetPlantRule.AllowedWaterLevelLow > altRelativeToWater || targetPlantRule.AllowedWaterLevelHigh < altRelativeToWater).ThrowIfTrue(ErrorCodes.InvalidConditionsForPlanting);
            }

            var plantRule = zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);
            if (plantRule != null)
            {
                plantRule.PlayerSeeded.ThrowIfTrue(ErrorCodes.HarvestableVegetationAlreadyExistsOnTile);
                
                //csak a hecc kedveert
                plantRule.HasBlockingState.ThrowIfTrue(ErrorCodes.InvalidConditionsForPlanting);
            }

            if (targetPlantRule.OnlyOnUnprotectedZone)
            {
                zone.Configuration.Protected.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);
            }

            if (targetPlantRule.AllowedOnNonNatural)
            {
                IsWallConditionsMatch(zone, x, y).ThrowIfFalse(ErrorCodes.TooManyAdjacentWalls);
                IsWallAmountMatch(zone, x, y).ThrowIfFalse(ErrorCodes.TooManyWallInArea);
            }
        }

        /// <summary>
        /// 
        ///     O
        ///    O+O
        ///     O
        /// 
        /// </summary>
        private static readonly int[] _nonDiagonalNeighbours = { 0, 1, 1, 0, 0, -1, -1, 0 };

        /// <summary>
        /// Checks planting conditions for a wall
        /// </summary>
        private bool IsWallConditionsMatch(IZone zone,int x, int y)
        {
            var plantCount = CountNonDiagonalPlants(zone, PlantType.Wall, x, y, x, y);

            if (plantCount > 3)
                return false;

            for (var i = 0; i <= 3; i++)
            {
                var xo = _nonDiagonalNeighbours[i * 2];
                var yo = _nonDiagonalNeighbours[i * 2 + 1];

                var tx = x + xo < zone.Size.Width ? x + xo : x;
                var ty = y + yo < zone.Size.Height ? y + yo : y;

                plantCount = CountNonDiagonalPlants(zone,PlantType.Wall, tx, ty, x, y);

                if (plantCount > 3)
                    return false;
                }

            return true;
        }


        /// <summary>
        /// Counts the non diagonal neighbouring plants of the given type
        /// </summary>
        private int CountNonDiagonalPlants(IZone zone,PlantType plantType, int x, int y, int origX, int origY)
        {
            var counter = 0;
            for (var i = 0; i <= 3; i++)
            {
                var xo = _nonDiagonalNeighbours[i * 2];
                var yo = _nonDiagonalNeighbours[i * 2 + 1];

                var tx = x + xo < zone.Size.Width ? x + xo : x;
                var ty = y + yo < zone.Size.Height ? y + yo : y;

                if (tx == origX && ty == origY)
                {
                    counter++;
                    continue;
                }

                var plantInfo = zone.Terrain.Plants.GetValue(tx, ty);

                if (plantInfo.type == plantType)
                {
                    counter++;
                }
            }

            return counter;
        }

        private const int WALL_AMOUNT_AREA_RADIUS = 10;
        private const int WALL_AMOUNT_MAXCOUNT = 95;

        /// <summary>
        /// Limits the amount of walls in an area
        /// </summary>
        private bool IsWallAmountMatch(IZone zone,int x, int y)
        {
            var area = zone.CreateArea(new Position(x, y), WALL_AMOUNT_AREA_RADIUS);
            return zone.CountPlantsInArea(PlantType.Wall, area) <= WALL_AMOUNT_MAXCOUNT;
        }
    }
}
