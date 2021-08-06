using System;
using Perpetuum.Log;

namespace Perpetuum.Zones.Terrains.Materials.Plants
{
    [Serializable]
    public struct PlantInfo : IEquatable<PlantInfo>
    {
        public PlantType type; //public
        public byte state; //public
        public byte time;
        public byte spawn;
        public byte health;
        public byte material; //public
        public GroundType groundType;

        public void Clear()
        {
            type = PlantType.NotDefined;
            state = 0;
            time = 0;
            health = 0;
            material = 0;
        }

        public void ClearGroundType()
        {
            SetGroundType(GroundType.undefined);
        }

        public void SetGroundType(GroundType type)
        {
            groundType = type;
        }

        public void SetPlant(byte newState, PlantType newPlantType)
        {
            state = newState;
            type = newPlantType;
        }

        public bool Equals(PlantInfo other)
        {
            return type == other.type &&
                   state == other.state &&
                   time == other.time &&
                   spawn == other.spawn &&
                   health == other.health &&
                   material == other.material &&
                   groundType == other.groundType;
        }

        public double GetHealthRatio(PlantRule myPlantRule)
        {
            var configHealth = myPlantRule.Health[state];

            if (configHealth == 0)
            {
                Logger.Error("max health is defined as 0 at rule: " + type);
                configHealth = 40;
            }

            return ((double)health).Ratio(configHealth);
        }

        public int HealPlant(PlantRule rule)
        {
            var configHealth = rule.Health[state];
            var oldHealth = health;

            if (health < configHealth)
            {
                var diff = ((configHealth - health) / 2.0).Clamp(0, 40);
                health = (byte)(Math.Ceiling(health + diff)).Clamp(0, configHealth);
            }

            return health - oldHealth;
        }

        public void UnHealPlant()
        {
            var amount = FastRandom.NextInt(3, 5);
            health = (byte)(health - amount).Clamp(0, 255);
        }

        public bool IsHealthOnMaximum(PlantRule rule)
        {
            var currentMaxHealth = rule.Health[state];
            return currentMaxHealth == health;
        }

        public bool IsPlantOnMaximumState(PlantRule rule)
        {
            return state == rule.GrowingStates.Count - 1;
        }

        public bool IsWallInLastFewStates(PlantRule rule)
        {
            return rule.GrowingStates.Count - state < 4;
        }

        public bool Check()
        {
            var isDefault = Equals(default(PlantInfo));
            var spawnIsZero = spawn == 0;
            return isDefault || spawnIsZero;
        }
    }
}
