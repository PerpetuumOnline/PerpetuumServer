using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Perpetuum.Log;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Materials.Plants.ExtensionsMethods;

namespace Perpetuum.Zones.Terrains
{
    [Serializable]
    public class NatureCube : IEquatable<NatureCube>
    {
        private readonly IZone _zone;
        private readonly Area _area;
        private PlantInfo[] _plantInfos;
        private BlockingInfo[] _blockInfos;

        private NatureCube() { }

        public NatureCube(IZone zone,Area area)
        {
            _zone = zone;
            _area = area;
            _plantInfos = zone.Terrain.Plants.GetArea(area);
            _blockInfos = zone.Terrain.Blocks.GetArea(area);
        }

        public NatureCube Clone()
        {
            return new NatureCube(_zone,_area)
            {
                _plantInfos = _plantInfos.ToArray(),
                _blockInfos = _blockInfos.ToArray()
            };
        }

        public void Commit()
        {
            _zone.Terrain.Plants.SetArea(_area, _plantInfos);
            _zone.Terrain.Blocks.SetArea(_area, _blockInfos);
        }

        public bool Equals(NatureCube other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (!_area.Equals(other._area))
                return false;

            return _plantInfos.SequenceEqual(other._plantInfos) && _blockInfos.SequenceEqual(other._blockInfos);
        }

        private delegate void CubeAction(int x, int y, ref PlantInfo plantInfo, ref BlockingInfo blockingInfo);

        private void ForEachInCube(CubeAction action)
        {
            for (var y = 0; y < _area.Height; y++)
            {
                for (var x = 0; x < _area.Width; x++)
                {
                    var offset = _area.GetOffset(x, y);

                    var plantInfo = _plantInfos[offset];
                    var blockInfo = _blockInfos[offset];

                    action(x, y, ref plantInfo, ref blockInfo);

                    _plantInfos[offset] = plantInfo;
                    _blockInfos[offset] = blockInfo;
                }
            }
        }

        private PlantInfo GetPlantInfo(int x, int y)
        {
            var offset = _area.GetOffset(x, y);
            return _plantInfos[offset];
        }

        private void SetPlantInfo(int x, int y, PlantInfo plantInfo)
        {
            var offset = _area.GetOffset(x, y);
            _plantInfos[offset] = plantInfo;
        }

        private BlockingInfo GetBlockInfo(int x, int y)
        {
            var offset = _area.GetOffset(x, y);
            return _blockInfos[offset];
        }

        private void SetBlockInfo(int x, int y, BlockingInfo blockingInfo)
        {
            var offset = _area.GetOffset(x, y);
            _blockInfos[offset] = blockingInfo;
        }

        public void ProcessAll()
        {
            //megnezi h korrektek-e a novenykek
            ValidatePlants();
            Check();
            //noveszti a novenyeket
            GrowPlants();
            Check();
            //tesz le ujakat ahova kell
            SpawnPlants();
            Check();
            //beallitja a biteket meg amit kell
            ValidatePlants(false);
            Check();
            //rohasztja a falakat
            DamageWall();
            Check();
            //kinyirja azokat amik nem nohetnek kozel
            KillOnDistance();
            Check();
            //noveszt egy kis materiat
            RenewMaterial();
            Check();
        }

        public void CorrectOnly()
        {
            //beallitja a biteket meg amit kell
            ValidatePlants(false);
            Check();
            //kinyirja azokat amik nem nohetnek kozel
            KillOnDistance(); //kill the plants that are too close
            Check();
        }

        #region Validate Plants

        private void ValidatePlants(bool cleanUpDeadPlants = true)
        {
            var waterLevel = ZoneConfiguration.WaterLevel / 4;

            for (var y = 0; y < _area.Height; y++)
            {
                for (var x = 0; x < _area.Width; x++)
                {
                    ValidateTile(waterLevel, x, y, cleanUpDeadPlants);
                }
            }
        }

        private void ValidateTile(int waterLevel, int x, int y, bool cleanUpDeadPlants = true)
        {
            var globalX = x + _area.X1;
            var globalY = y + _area.Y1;

            var plantInfo = GetPlantInfo(x, y);
            var blockInfo = GetBlockInfo(x, y);

            //checking different layer conditions and correct them
            //----------------------------------------------------------------------------

            //decor, obstacle, island
            if (blockInfo.NonNaturally)
            {
                //clean only the plant data, keep the blocking height
                CleanPlantOnTile(x, y, false);
                return;
            }

            if (blockInfo.Height > 0 && blockInfo.Flags == BlockingFlags.Undefined)
            {
                //clean blocking height
                blockInfo.Height = 0;
                SetBlockInfo(x, y, blockInfo);
            }

            //no decor, obstace, or island is on the tile

            //illegal plantbyte - this controls the animation on the client call it only once per cube process
            if (cleanUpDeadPlants)
            {
                if (plantInfo.type != 0 || plantInfo.state == 0)
                    return;

                //clean full
                CleanPlantOnTile(x, y);
                return;
            }

            if (plantInfo.material > 0 && plantInfo.type == 0)
            {
                CleanPlantOnTile(x, y);
                return;
            }

            //if the plant is 0 AND the plant blocking bit is 1 then fix it
            if (plantInfo.type == 0 && blockInfo.Plant)
            {
                CleanPlantOnTile(x, y);
                return;
            }

            if (_zone.Terrain.Plants.GetValue(globalX, globalY).spawn == 0)
            {
                //non fertile tile
                CleanPlantOnTile(x, y);
                return;
            }

            if (plantInfo.type == 0)
                return;

            var plantRule = _zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);

            //if the index is unknown AND it's not 0 then kill the plant
            if (plantRule == null)
            {
                //no such index
                CleanPlantOnTile(x, y);
                return;
            }

            //ok, check the plant rule
            //----------------------------------------------------------------------------

            var controlCurrVal = _zone.Terrain.Controls.GetValue(globalX, globalY);

            if (!plantRule.AllowedOnNonNatural)
            {
                //normal plant check control bits

                if (!plantRule.PlacesConcrete)
                {
                    if (!controlCurrVal.PlantAllowed)
                    {
                        //plants can't extist on tiles with road
                        CleanPlantOnTile(x, y);
                        return;
                    }
                }
                else
                {
                    //devrinol places concrete
                    if (!controlCurrVal.DevrinolAllowed)
                    {
                        CleanPlantOnTile(x, y);
                        return;
                    }
                }
            }

            //correct the blocking height if it's needed
            var blockingHeight = plantRule.GetBlockingHeight(plantInfo.state);

            if (blockingHeight > 0 && blockInfo.Height != blockingHeight)
            {
                blockInfo.Height = blockingHeight;
                SetBlockInfo(x, y, blockInfo);
            }

            if (!plantRule.GrowingStates.ContainsKey(plantInfo.state))
            {
                //no such state
                CleanPlantOnTile(x, y);
                return;
            }

            var altitudeValue = _zone.Terrain.Altitude.GetAltitude(globalX, globalY) / 4; // /4 to match with the client's coordinates

            if (plantRule.AllowedAltitudeLow > altitudeValue || plantRule.AllowedAltitudeHigh < altitudeValue)
            {
                //allowed altitude check failed
                CleanPlantOnTile(x, y);
                return;
            }

            var altRelativeToWater = altitudeValue - waterLevel;
            //                30
            if (plantRule.AllowedWaterLevelLow > altRelativeToWater || plantRule.AllowedWaterLevelHigh < altRelativeToWater)
            {
                //water level distance failed
                CleanPlantOnTile(x, y);
                return;
            }

            if (!plantRule.AllowedOnNonNatural)
            {
                var terrainIndex = _zone.Terrain.Plants.GetValue(globalX, globalY).groundType;

                if (!plantRule.AllowedTerrainTypes.Contains(terrainIndex))
                {
                    //not allowed terrain type
                    CleanPlantOnTile(x, y);
                    return;
                }
            }

            //plant state is blocking and the plant blocking bit is 0
            if (plantRule.IsBlocking(plantInfo.state) && !blockInfo.Plant)
            {
                //fix blocking bit to 1
                blockInfo.Plant = true;

                //set blocking height
                blockInfo.Height = plantRule.GetBlockingHeight(plantInfo.state);

                SetBlockInfo(x, y, blockInfo);

            }

            //plant is not blocking and the plant blocking bit is 1
            if (!plantRule.IsBlocking(plantInfo.state) && blockInfo.Plant)
            {
                //fix blocking bit to 0
                blockInfo.Plant = false;

                //set blocks layer to 0
                blockInfo.Height = 0;
                SetBlockInfo(x, y, blockInfo);
            }

            if (!plantRule.AllowedOnNonNatural)
            {
                var terrainSlope = _zone.Terrain.Slope.GetValue(globalX, globalY);
                if (terrainSlope > plantRule.Slope || terrainSlope < plantRule.MinSlope)
                {
                    //terrain too steep
                    CleanPlantOnTile(x, y);
                    return;
                }
            }

            //not fruiting -> clear material
            if (plantRule.NotFruiting)
            {
                plantInfo.material = 0;
                SetPlantInfo(x, y, plantInfo);
            }
            else
            {
                if (plantInfo.state < plantRule.FruitingState)
                {
                    plantInfo.material = 0;
                    SetPlantInfo(x, y, plantInfo);
                }

                if (plantInfo.material == 0 && plantInfo.type != 0 && plantRule.FruitingState <= plantInfo.state)
                {
                    //this plant should have material 
                    CleanPlantOnTile(x, y);
                }
            }
        }
        #endregion

        #region Grow Plants

        private void GrowPlants()
        {
            ForEachInCube((int x, int y, ref PlantInfo plantInfo, ref BlockingInfo blockingInfo) => GrowPlantOnTile(x, y, ref plantInfo, ref blockingInfo));
        }

        private void GrowPlantOnTile(int x, int y, ref PlantInfo plantInfo, ref BlockingInfo blockInfo)
        {
            var plantRule = _zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);
            if (plantRule == null)
                return;

            if (plantInfo.time < plantRule.GrowRate)
            {
                //increase tile time
                plantInfo.time = (byte)(plantInfo.time + 1).Clamp(0, 255);
            }
            else
            {
                //make it grow!
                //Reset time
                plantInfo.time = 0;

                //get new state
                PlantType nextAction;
                byte nextState;

                plantRule.GetNextState(plantInfo.state, out nextState, out nextAction);

                if (nextAction == 0)
                {
                    plantInfo.Clear();
                    plantInfo.state = 1; //kill signal for client --> resulting type:0 state:1 which should NOT be cleaned, just next round!!!
                    blockInfo.Height = 0;
                    blockInfo.Plant = false;

                    if (!plantRule.PlacesConcrete)
                        return;

                    var gx = x + _area.X1;
                    var gy = y + _area.Y1;
                    _zone.Terrain.Controls.UpdateValue(gx,gy,ci =>
                    {
                        ci.ClearAllConcrete();
                        return ci;
                    });
                }
                else
                {
                    var healthRatio = plantInfo.GetHealthRatio(plantRule);

                    plantInfo.type = nextAction;
                    plantInfo.state = nextState;
                    plantInfo.health = (byte)(healthRatio * plantRule.Health[plantInfo.state]).Clamp(1, 255);

                    if (!plantRule.NotFruiting)
                    { 
                        //yes, fruiting

                        if (plantRule.FruitingState <= nextState)
                        {
                            if (plantRule.FruitingState == nextState)
                            {
                                // first phase when the plant reached fruiting state
                                plantInfo.material = (byte)(Math.Min((int)(FastRandom.NextDouble(plantRule.FruitAmount * 0.05, plantRule.FruitAmount * 0.15)).Clamp(0, 255), plantRule.FruitAmount));
                            }
                            else
                            {
                                // change happened
                                plantInfo.material = (byte)(Math.Min((plantInfo.material + FastRandom.NextDouble(plantRule.FruitAmount * 0.15, plantRule.FruitAmount * 0.25)), plantRule.FruitAmount)).Clamp(0, 255);
                            }
                        }
                        else
                        {
                            plantInfo.material = 0;
                        }
                    }
                    else
                    {
                        plantInfo.material = 0;
                    }

                    blockInfo.Height = plantRule.GetBlockingHeight(plantInfo.state);
                    blockInfo.Plant = blockInfo.Height > 0;
                }
            }
        }
        #endregion

        #region Spawn Plants

        private void SpawnPlants()
        {
            var plantAmount = _plantInfos.CountPlants();
            var plantsInCube = 0;

            foreach (var kvp in plantAmount)
            {
                var currentPlantType = kvp.Key;
                var currentAmount = kvp.Value;

                var rule = _zone.Configuration.PlantRules.GetPlantRule(currentPlantType);

                if (rule.HasBlockingState)
                {
                    plantsInCube += currentAmount;
                }
            }

            var tilesInCube = _area.Ground;

            var fertilityFactor = _zone.Configuration.Fertility / 100.0;

            var availablePlaces = (int)(tilesInCube * fertilityFactor);
            availablePlaces -= plantsInCube;

            var cubeFertilityState = plantsInCube / (double)tilesInCube;

            //zone fertility check
            if (cubeFertilityState > fertilityFactor)
            {
                //ok, cube is fulfilling the fertility condition
                return;
            }

            for (var i = 0; i < tilesInCube; i++)
            {
                var x = FastRandom.NextInt(_area.Width - 1);
                var y = FastRandom.NextInt(_area.Height - 1);

                var globalX = x + _area.X1;
                var globalY = y + _area.Y1;

                var plantInfo = GetPlantInfo(x, y);
                var blockInfo = GetBlockInfo(x, y);

                var plantType = plantInfo.type;

                if (plantType != 0 || blockInfo.Flags != BlockingFlags.Undefined)
                    continue;

                //per tile random chance
                if (FastRandom.NextByte() >= plantInfo.spawn)
                    continue;

                var newPlantRule = GetNewPlantRule(globalX, globalY);

                if (newPlantRule == null)
                {
                    plantInfo.Clear();
                    blockInfo.Height = 0;
                    blockInfo.Plant = false;

                    SetPlantInfo(x, y, plantInfo);
                    SetBlockInfo(x, y, blockInfo);
                    continue;
                }

                //is there a max amount defined for the current type?
                if (newPlantRule.MaxAmount > 0)
                {
                    //have we planted enough?
                    var amount = plantAmount.GetOrDefault(newPlantRule.Type);
                    if (amount > newPlantRule.MaxAmount)
                        continue;
                }

                if (!CheckKillDistance(newPlantRule, x, y, this))
                {
                    plantInfo.Clear();
                    blockInfo.Plant = false;
                    blockInfo.Height = 0;

                    SetPlantInfo(x, y, plantInfo);
                    SetBlockInfo(x, y, blockInfo);
                    continue;
                }

                plantInfo.type = newPlantRule.Type;
                plantInfo.state = 0;
                plantInfo.material = 0; //no material at state0
                plantInfo.health = newPlantRule.Health[plantInfo.state]; //set health

                blockInfo.Height = newPlantRule.GetBlockingHeight(plantInfo.state);

                if (blockInfo.Height > 0)
                    blockInfo.Plant = true;

                SetPlantInfo(x, y, plantInfo);
                SetBlockInfo(x, y, blockInfo);

                if (--availablePlaces <= 0)
                {
                    return;
                }
            }
        }

        #endregion

        #region Damage Wall

        private void DamageWall()
        {
            if (_zone.Configuration.IsAlpha) 
                return; //nothing to do on alpha

            //on gamma we have to collect pbs data
            if (_zone.Configuration.IsGamma)
            {
                var searchRadius = DistanceConstants.TERRAIN_DEGRADE_DISTANCE_FROM_PBS+Math.Max(_area.Width,_area.Height);
                _intactDistance = (int)(DistanceConstants.TERRAIN_DEGRADE_DISTANCE_FROM_PBS*PBSHelper.DEGRADE_NEAR_BIAS);
                _wallDistanceFromPBS = (int)DistanceConstants.TERRAIN_DEGRADE_DISTANCE_FROM_PBS;
                _gradientRange = _wallDistanceFromPBS - _intactDistance;
                _pbsPositions = _zone.GetStaticUnits().Where(o => o is IPBSObject && o.CurrentPosition.TotalDistance2D(_area.Center) < searchRadius).Select(o => o.CurrentPosition).ToList(); 
            }
            

            //on beta and gamma it will run    
            ForEachInCube((int x, int y, ref PlantInfo plantInfo, ref BlockingInfo blockingInfo) => DamageWallOnTile(ref plantInfo, ref blockingInfo, x, y));

        }

        private int _gradientRange;
        private int _intactDistance;
        private int _wallDistanceFromPBS;
        private List<Position> _pbsPositions;


        private void DamageWallOnTile(ref PlantInfo plantInfo, ref BlockingInfo blockInfo,int x,int y )
        {
            if (plantInfo.type == PlantType.NotDefined) 
                return;

            var plantRule = _zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);
            if (plantRule == null)
                return;

            //is it wall?
            if (!plantRule.AllowedOnNonNatural)
                return;

            //only degrades in the last few phases
            if (!plantInfo.IsWallInLastFewStates(plantRule))
                return;

            var globalX = x + _area.X1;
            var globalY = y + _area.Y1;

            //pbs might save it
            if (_pbsPositions != null && _pbsPositions.Count > 0)
            {
                if (PBSHelper.IsPBSInRange(_wallDistanceFromPBS, _pbsPositions, globalX, globalY))
                {
                    var closestPBSDistance = PBSHelper.GetMinimalDistance(_pbsPositions, globalX, globalY);

                    if (closestPBSDistance <= _intactDistance)
                    {
                        //pbs object in intact distance, wall is protected
                        return;
                    }

                    //in fading range    

                    //near 1 .. far 0
                    var chanceToSurvive = 1 -( (closestPBSDistance - _intactDistance) /  _gradientRange);

                    var random = FastRandom.NextDouble();

                    if ( random < chanceToSurvive)
                    {
                        //it will happen inwards more often  
                        
                        //so the plant wont get unhealed as it is closer to a pbs but within the range
                        return;
                    }
                }
            }
           
           

            plantInfo.UnHealPlant();

            if (plantInfo.health > 0)
                return;

            blockInfo.Plant = false;
            blockInfo.Height = 0;

            plantInfo.Clear();
            plantInfo.state = 1;
        }

        #endregion

        #region Kill on distance

        private void KillOnDistance()
        {
            var size = _zone.Size;

            for (var i = 0; i < _area.Ground * 2; i++)
            {
                var x = FastRandom.NextInt(_area.Width - 1);
                var y = FastRandom.NextInt(_area.Height - 1);

                var globalX = x + _area.X1;
                var globalY = y + _area.Y1;

                var plantInfo = GetPlantInfo(x, y);

                var plantRule = _zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);
                if (plantRule == null)
                    continue;

                if (plantRule.KillDistance <= 0)
                    continue;

                for (var sy = globalY - plantRule.KillDistance; sy < globalY + plantRule.KillDistance; sy++)
                {
                    for (var sx = globalX - plantRule.KillDistance; sx < globalX + plantRule.KillDistance; sx++)
                    {
                        if (sx < 0 || sx >= size.Width || sy < 0 || sy >= size.Height)
                            continue;

                        //not itself
                        if (sx - _area.X1 == x && sy - _area.Y1 == y)
                            continue;

                        var searchPlantType = _area.Contains(sx, sy)
                            ? GetPlantInfo(sx - _area.X1, sy - _area.Y1).type //sample from the cube
                            : _zone.Terrain.Plants.GetValue(sx, sy).type; //sample from the real map

                        //is there a plant we are looking for?
                        if (plantInfo.type != searchPlantType)
                            continue;

                        if (!IsCloserThan(globalX, globalY, sx, sy, plantRule.KillDistance))
                            continue;

                        //we found a plant which is too close
                        //kill the original
                        var blockInfo = GetBlockInfo(x, y);

                        plantInfo.Clear();
                        blockInfo.Height = 0;
                        blockInfo.Plant = false;

                        SetPlantInfo(x, y, plantInfo);
                        SetBlockInfo(x, y, blockInfo);
                    }
                }
            }
        }

        #endregion

        #region Renew Material

        private void RenewMaterial()
        {
            ForEachInCube((int x, int y, ref PlantInfo plantInfo, ref BlockingInfo blockingInfo) => RenewMaterial(ref plantInfo));
        }

        private void RenewMaterial(ref PlantInfo plantInfo)
        {
            var plantRule = _zone.Configuration.PlantRules.GetPlantRule(plantInfo.type);
            if (plantRule == null)
                return;

            if (plantRule.NotFruiting || !plantRule.IsBlocking(plantInfo.state))
                return;

            if (plantRule.FruitingState > plantInfo.state)
                return;

            if (plantInfo.material >= plantRule.FruitAmount)
                return;

            var chance = FastRandom.NextDouble();
            if (chance >= 0.3)
                return;

            var m = (int) plantInfo.material;
            m = (byte) (Math.Min(m + (int) FastRandom.NextDouble(plantRule.FruitAmount*0.05, plantRule.FruitAmount*0.15).Clamp(0, 255), plantRule.FruitAmount));
            plantInfo.material = (byte) m.Clamp(0, 255);
        }

        #endregion

        private void CleanPlantOnTile(int x, int y, bool cleanBlockingHeight = true)
        {
            var plantInfo = GetPlantInfo(x, y);
            var blockInfo = GetBlockInfo(x, y);

            var oldPlantType = plantInfo.type;

            plantInfo.type = 0;
            plantInfo.state = 0;
            plantInfo.time = 0;
            plantInfo.health = 0;
            plantInfo.material = 0;

            //clear plant blocking bit
            blockInfo.Plant = false;

            if (cleanBlockingHeight)
            {
                blockInfo.Height = 0;
            }

            SetPlantInfo(x, y, plantInfo);
            SetBlockInfo(x, y, blockInfo);

            if (oldPlantType != PlantType.Devrinol)
                return;

            var gx = x + _area.X1;
            var gy = y + _area.Y1;

            _zone.Terrain.Controls.UpdateValue(gx,gy,ci =>
            {
                ci.ClearAllConcrete();
                return ci;
            });
        }

        private PlantRule[] _allPlantRules;
        private List<PlantType> _playerSeededTypes;

        private PlantRule[] GetAllPlantRules
        {
            get
            {
                if (_allPlantRules == null)
                {
                    _allPlantRules = _zone.Configuration.PlantRules.ToArray();
                }

                return _allPlantRules;
            }
        }

        private List<PlantType> GetPlayerSeededTypes
        {
            get
            {
                if (_playerSeededTypes == null)
                {
                    _playerSeededTypes = GetAllPlantRules.Where(r => r.PlayerSeeded).Select(r => r.Type).ToList();

                }

                return _playerSeededTypes;
            }
        }



        [CanBeNull]
        private PlantRule GetNewPlantRule(int globalX, int globalY)
        {
            var area = Area.FromRadius(globalX, globalY, _neighbourRadius);
            var excludedTypes = GetPlayerSeededTypes;

            var neighbouringPlants = GetPlantTypeCountFromRange(area,excludedTypes);

            PlantType resultPlantType;

            if (neighbouringPlants.Count == 0)
            {
                //no plants, then fertility decides
                resultPlantType = GetAllPlantRules.GetWinnerPlantTypeBasedOnFertility();
            }
            else
            {
                var rulingPlant = neighbouringPlants.OrderBy(k => k.Value).Last().Key;

                var plantTypeBySpreading = GetAllPlantRules.GetSpreadingBasedWinnerPlantType(neighbouringPlants);

                //50% currently ruling plant OR spreading types
                resultPlantType = FastRandom.NextDouble() < 0.5 ? rulingPlant : plantTypeBySpreading;


            }

            return GetAllPlantRules.FirstOrDefault(p => p.Type == resultPlantType);
            
        }

        private bool CheckKillDistance(PlantRule rule, int x, int y, NatureCube cube)
        {
            if (rule.KillDistance <= 0)
                return true;

            var globalX = x + cube._area.X1;
            var globalY = y + cube._area.Y1;
            var size = _zone.Size;

            for (var j = globalY - rule.KillDistance; j < globalY + rule.KillDistance; j++)
            {
                for (var i = globalX - rule.KillDistance; i < globalX + rule.KillDistance; i++)
                {
                    //exit if outside of the map
                    if (i < 0 || i >= size.Width || j < 0 || j >= size.Height) 
                        continue;

                    PlantType plantType;
                    if (cube._area.Contains(i, j))
                    {
                        //sample from the cube, convert back the coordinate to cube local
                        var cx = i - cube._area.X1;
                        var cy = j - cube._area.Y1;

                        plantType = cube.GetPlantInfo(cx, cy).type;
                    }
                    else
                    {
                        //sample from the real map
                        plantType = _zone.Terrain.Plants.GetValue(i, j).type;
                    }

                    if (plantType == rule.Type && IsCloserThan(globalX, globalY, i, j, rule.KillDistance))
                        return false;
                }
            }

            return true;
        }

        private static bool IsCloserThan(int x1, int y1, int x2, int y2, double distance)
        {
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) < distance * distance;
        }

        private readonly int[] _neighbours =
        {
            -1, -1,
            -1, 0,
            -1, 1,
            0, -1,
            0, 1,
            1, -1,
            1, 0,
            1, 1,

            -2, 2,
            -1, 2,
            0, 2,
            1, 2,
            2, 2,

            2, 1,
            2, 0,
            2, -1,
            2, -2,

            1, -2,
            0, -2,
            -1, -2,
            -2, -2,

            -2, -1,
            -2, 0,
            -2, 1
        };

       

        private const int _neighbourRadius = 4;

        private Dictionary<PlantType, int> GetPlantTypeCountFromRange( Area area, List<PlantType> excludedTypes)
        {
            var result = new Dictionary<PlantType, int>();
            
            area = area.Clamp(_zone.Size);

            area.ForEachXY((x, y) =>
            {
                var plantType = _zone.Terrain.Plants.GetValue(x, y).type;
                if (plantType != PlantType.NotDefined)
                {
                    if (!excludedTypes.Contains(plantType))
                    {
                        result.AddOrUpdate(plantType, 1, c => ++c);
                    }
                }
            });
            
            return result;

        }
            
            
            
        [Conditional("DEBUG")]
        public void Check()
        {
            var x = _plantInfos.Any(pi => pi.Check());
            if (x)
            {
                Logger.Error("empty naturecube " + _zone.Id + " " + _area + " " + SystemTools.GetCallStack());
            }

            var checkSpawnAmount = _plantInfos.Count(plantInfo => plantInfo.spawn > 0) >= 10;
            if (!checkSpawnAmount)
            {
                Logger.Error("no spawn was found.  " + _zone.Id + " " + _area);
            }
        }
    }
}