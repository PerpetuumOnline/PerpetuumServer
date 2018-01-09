using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.IDGenerators;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public class FlockConfigurationBuilder
    {
        private readonly IEntityDefaultReader _entityDefaultReader;
        private readonly FlockConfiguration _configuration;

        public delegate FlockConfigurationBuilder Factory();

        public FlockConfigurationBuilder(IEntityDefaultReader entityDefaultReader)
        {
            _entityDefaultReader = entityDefaultReader;
            _configuration = new FlockConfiguration();
        }

        public FlockConfigurationBuilder WithIDGenerator(IIDGenerator<int> idGenerator)
        {
            _configuration.ID = idGenerator.GetNextID();
            return this;
        }

        public FlockConfigurationBuilder With(Action<FlockConfiguration> action)
        {
            action(_configuration);
            return this;
        }

        public FlockConfigurationBuilder WithDefinition(int definition)
        {
            _configuration.EntityDefault = _entityDefaultReader.Get(definition);
            return this;
        }

        public FlockConfigurationBuilder SetHomeRange(int homeRange)
        {
            _configuration.HomeRange = homeRange.Max((int) DistanceConstants.MAX_NPC_FLOCK_HOME_RANGE);
            return this;
        }

        public FlockConfigurationBuilder SetID(int id)
        {
            _configuration.ID = id;
            return this;
        }

        public FlockConfigurationBuilder FromDictionary(Dictionary<string, object> dictionary)
        {
            return With(c =>
            {
                c.Name = (string) dictionary[k.name];
                c.PresenceID = (int) dictionary[k.presenceID];
                c.FlockMemberCount = (int) dictionary[k.flockMemberCount];
                c.SpawnOrigin = (Position) dictionary[k.spawnOrigin];
                c.SpawnRange = new IntRange((int) dictionary[k.spawnRangeMin], (int) dictionary[k.spawnRangeMax]);
                c.RespawnTime = TimeSpan.FromSeconds((int) dictionary[k.respawnSeconds]);
                c.TotalSpawnCount = (int) dictionary[k.totalSpawnCount];
                c.RespawnMultiplierLow = (double) dictionary[k.respawnMultiplierLow];
            })
            .SetHomeRange((int)dictionary[k.homeRange])
            .WithDefinition((int)dictionary[k.definition]);
        }

        public IFlockConfiguration Build()
        {
            return _configuration;
        }
    }
}