using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Terrains.Materials.Plants
{
    /// <summary>
    /// This controls the growth of a plant
    /// </summary>
    public class PlantRule
    {
        private readonly IDictionary<string, object> _settings;
       
        public Dictionary<byte, PlantPhase> GrowingStates { get; private set; }

        public struct PlantPhase
        {
            public byte[] state;
            public PlantType[] action;
        }

        public PlantRule(IDictionary<string,object> settings)
        {
            _settings = settings;

            AllowedTerrainTypes = _settings.GetOrDefault(k.allowedTerrainTypes, new int[0]).Select(t => (GroundType)t).OrderBy(t => t).ToArray();

            var i = 0;

            GrowingStates = new Dictionary<byte, PlantPhase>();

            while (settings.ContainsKey($"state_{i}"))
            {
                var states = (int[])settings[$"state_{i}"];
                var actions = (int[])settings[$"action_{i}"];

                var plantPhase = new PlantPhase
                {
                    state = states.Select(s => (byte) s).ToArray(),
                    action = actions.Select(a => (PlantType) a).ToArray()
                };

                GrowingStates.Add((byte)i, plantPhase);
                i++;
            }

            CheckConsistency();
        }

        public PlantType Type
        {
            get { return (PlantType) _settings.GetOrDefault(k.index,0); }
        }

        private string Name
        {
            get { return _settings.GetOrDefault(k.name,string.Empty); }
        }

        //controls the occurence of a plant
        public int Fertility
        {
            get { return _settings.GetOrDefault(k.fertility, 0); }
        }

        //controls if the plant prefers growing in groups
        public int Spreading
        {
            get { return _settings.GetOrDefault(k.spreading, 0); }
        }

        //no other plant of the same type can be found within this distance
        public int KillDistance
        {
            get { return _settings.GetOrDefault(k.killDistance, 0); }
        }

        //limits the maximum slope the plant can grow on
        public int Slope
        {
            get { return _settings.GetOrDefault(k.slope, 0); }
        }

        //controls the amount of cycles the plant has to skip to grow
        public int GrowRate
        {
            get { return _settings.GetOrDefault(k.growRate, 0); }
        }

        //lowest allowed altitude
        public int AllowedAltitudeLow
        {
            get { return _settings.GetOrDefault(k.allowedAltitudeLow, 0); }
        }

        //highest allowed altitude
        public int AllowedAltitudeHigh
        {
            get { return _settings.GetOrDefault(k.allowedAltitudeHigh, 0); }
        }

        //lowest allowwed altitude from waterlevel
        public int AllowedWaterLevelLow
        {
            get { return _settings.GetOrDefault(k.allowedWaterLevelLow, 0); }
        }

        //highest allowwed altitude from waterlevel - to create vegetation near the shore
        public int AllowedWaterLevelHigh
        {
            get { return _settings.GetOrDefault(k.allowedWaterLevelHigh, 0); }
        }

        //the state from which the plant produces fruit
        public int FruitingState
        {
            get { return _settings.GetOrDefault(k.fruitingState, -1); }
        }

        //not harvestable
        public bool NotFruiting
        {
            get { return FruitingState < 0; }
        }

        //the type of the fruit
        public int FruitDefinition
        {
            get { return _settings.GetOrDefault(k.fruitDefinition, -1); }
        }

        //material amount in layer - amount of cycles
        public byte FruitAmount
        {
            get { return (byte)_settings.GetOrDefault(k.fruitAmount, 0).Clamp(0, 255); }
        }

        //the terrain types the plant is allowed to grow on
        public IEnumerable<GroundType> AllowedTerrainTypes { get; private set; }

        //blocking heights for the growing states
        [NotNull]
        private int[] BlockingHeight
        {
            get { return _settings.GetOrDefault(k.blockingHeight,new int[0]); }
        }

        //health values for each growing state
        public byte[] Health
        {
            get { return _settings.GetOrDefault(k.health, new int[0]).Select(i => (byte)i.Clamp(0,255)).ToArray(); }
        }

        //fruit definition name
        public string FruitMaterialName
        {
            get { return _settings.GetOrDefault(k.fruitMaterialName, string.Empty); }
        }

        //the amount of plant per processing area (cube)
        public int MaxAmount
        {
            get { return _settings.GetOrDefault(k.maxAmount, -1); }
        }

        //player must plant this type of plant
        public bool PlayerSeeded
        {
            get { return _settings.GetOrDefault(k.playerSeeded, 0).ToBool(); }
        }

        //the minimal allowed slope for a plant
        public int MinSlope
        {
            get { return _settings.GetOrDefault(k.minSlope, 0); }
        }

        //damage resistance of the plant
        public double DamageScale
        {
            get { return _settings.GetOrDefault(k.damageScale, 1.0d); }
        }

        //only on beta and gamma zones
        public bool OnlyOnUnprotectedZone
        {
            get { return _settings.GetOrDefault(k.onlyOnUnprotectedZone, 0).ToBool(); }
        }

        //can grow anywhere
        public bool AllowedOnNonNatural
        {
            get { return _settings.GetOrDefault(k.allowedOnNonNatural, 0).ToBool(); }
        }

        //handles the concrete layer below itself - places when born - removes when dies
        public bool PlacesConcrete
        {
            get { return _settings.GetOrDefault(k.placesConcrete, 0).ToBool(); }
        }

        /// <summary>
        /// Returns the plants next growing state
        /// </summary>
        /// <param name="state">current state</param>
        /// <param name="nextState">the next calculated state</param>
        /// <param name="nextAction">the next calculated type</param>
        public void GetNextState(byte state, out byte nextState, out PlantType nextAction)
        {
            nextState = GrowingStates[state].state[FastRandom.NextInt(GrowingStates[state].state.Length-1)];
            nextAction = GrowingStates[state].action[FastRandom.NextInt(GrowingStates[state].action.Length-1)];
        }

        public bool IsBlocking(byte state)
        {
            return GetBlockingHeight(state) > 0;
        }

        public byte GetBlockingHeight(byte state)
        {
            if (BlockingHeight.Length > state)
            {
                return (byte)BlockingHeight[state];
            }

            return 0;
        }

        public bool HasBlockingState
        {
            get { return BlockingHeight.Any(v => v > 0); }
        }

        public Dictionary<string, object > ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.index, (int) Type},
                           {k.mineral, FruitDefinition},
                           {k.name, Name},
                           {k.blocks, BlockingHeight},
                           {k.health, Health.Select(v=>(int)v).ToArray()}
                       };
        }

        private void CheckConsistency()
        {
            Health.Length.ThrowIfNotEqual(BlockingHeight.Length, () => new InvalidOperationException("health:" + Health.Length + " blocking:" + BlockingHeight.Length));
            Health.Length.ThrowIfNotEqual(GrowingStates.Count, () => new InvalidOperationException("health:" + Health.Length + " states:" + GrowingStates.Count));
        }

        public override string ToString()
        {
            return $"Type: {Type}, Name: {Name}";
        }
    }
}
