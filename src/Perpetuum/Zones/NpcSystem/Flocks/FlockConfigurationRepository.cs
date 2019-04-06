using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public class FlockConfigurationRepository : IFlockConfigurationRepository
    {
        private readonly FlockConfigurationBuilder.Factory _flockConfigurationBuilderFactory;
        private readonly Dictionary<int,IFlockConfiguration> _flockConfigurations = new Dictionary<int, IFlockConfiguration>();

        public FlockConfigurationRepository(FlockConfigurationBuilder.Factory flockConfigurationBuilderFactory)
        {
            _flockConfigurationBuilderFactory = flockConfigurationBuilderFactory;
        }

        public void LoadAllConfig()
        {
            var records = Db.Query().CommandText("select * from npcflock").Execute();
            foreach (var r in records)
            {
                var builder = _flockConfigurationBuilderFactory();
                builder.With(c =>
                {
                    c.ID = r.GetValue<int>("id");
                    c.Name = r.GetValue<string>("name");
                    c.PresenceID = r.GetValue<int>("presenceid");
                    c.FlockMemberCount = r.GetValue<int>("flockmembercount");
                    c.EntityDefault = EntityDefault.Get(r.GetValue<int>("definition"));
                    c.SpawnOrigin = new Position(r.GetValue<int>("spawnoriginX"), r.GetValue<int>("spawnoriginY"));
                    c.SpawnRange = new IntRange(r.GetValue<int>("spawnrangeMin"), r.GetValue<int>("spawnrangeMax"));
                    c.RespawnTime = TimeSpan.FromSeconds(r.GetValue<int>("respawnseconds"));
                    c.TotalSpawnCount = r.GetValue<int>("totalspawncount");
                    c.HomeRange = r.GetValue<int>("homerange");
                    c.Note = r.GetValue<string>("note");
                    c.RespawnMultiplierLow = r.GetValue<double>("respawnmultiplierlow");
                    c.IsCallForHelp = r.GetValue<bool>("iscallforhelp");
                    c.Enabled = r.GetValue<bool>("enabled");
                    c.BehaviorType = (NpcBehaviorType) r.GetValue<int>("behaviorType");
                    c.SpecialType = (NpcSpecialType) r.GetValue<int>("npcSpecialType");
                });
                var config = builder.Build();
                _flockConfigurations[config.ID] = config;
            }
        }

        public IEnumerable<IFlockConfiguration> GetAllByPresence(int presenceID)
        {
            return _flockConfigurations.Values.Where(t => t.PresenceID == presenceID).ToArray();
        }

        public IFlockConfiguration Get(int flockID)
        {
            return _flockConfigurations.GetOrDefault(flockID);
        }

        public IEnumerable<IFlockConfiguration> GetAll()
        {
            return _flockConfigurations.Values;
        }

        public void Insert(IFlockConfiguration item)
        {
            const string query = @"insert npcflock 
                                   (name,presenceid,flockmembercount,definition,spawnoriginX,spawnoriginY,spawnrangeMin,spawnrangeMax,respawnseconds,totalspawncount,homerange,note,respawnmultiplierlow) values 
                                   (@name,@presenceID,@flockMemberCount,@definition,@spawnOriginX,@spawnOriginY,@spawnRangeMin,@spawnRangeMax,@respawnSeconds,@totalSpawnCount,@homeRange,@note,@respawnMultiplierLow);
                                   select cast(scope_identity() as int)";
            var id = Db.Query().CommandText(query)
                .SetParameter("@name", item.Name)
                .SetParameter("@presenceID", item.PresenceID)
                .SetParameter("@flockMemberCount", item.FlockMemberCount)
                .SetParameter("@definition", item.EntityDefault.Definition)
                .SetParameter("@spawnOriginX", item.SpawnOrigin.intX)
                .SetParameter("@spawnOriginY", item.SpawnOrigin.intY)
                .SetParameter("@spawnRangeMin", item.SpawnRange.Min)
                .SetParameter("@spawnRangeMax", item.SpawnRange.Max)
                .SetParameter("@respawnSeconds", item.RespawnTime.Seconds)
                .SetParameter("@totalSpawnCount", item.TotalSpawnCount)
                .SetParameter("@homeRange", item.HomeRange)
                .SetParameter("@respawnMultiplierLow", item.RespawnMultiplierLow)
                .SetParameter("@note", item.Note)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            if (item is FlockConfiguration fc)
                fc.ID = id;
        }

        public void Update(IFlockConfiguration item)
        {
            const string query = @"update npcflock 
                                      set [name]=@name,
                                          presenceid=@presenceID,
                                          flockmembercount=@flockMemberCount,
                                          definition=@definition,
                                          spawnoriginX=@spawnOriginX,
                                          spawnoriginY=@spawnOriginY,
                                          spawnrangeMin=@spawnRangeMin,
                                          spawnrangeMax=@spawnRangeMax,
                                          respawnseconds=@respawnSeconds,
                                          totalspawncount=@totalSpawnCount,
                                          homerange=@homeRange,
                                          respawnmultiplierlow=@respawnMultiplierLow
                                      where id=@ID";

            var res = Db.Query().CommandText(query)
                    .SetParameter("@ID", item.ID)
                    .SetParameter("@name", item.Name)
                    .SetParameter("@presenceID", item.PresenceID)
                    .SetParameter("@flockMemberCount", item.FlockMemberCount)
                    .SetParameter("@definition", item.EntityDefault.Definition)
                    .SetParameter("@spawnOriginX", item.SpawnOrigin.intX)
                    .SetParameter("@spawnOriginY", item.SpawnOrigin.intY)
                    .SetParameter("@spawnRangeMin", item.SpawnRange.Min)
                    .SetParameter("@spawnRangeMax", item.SpawnRange.Max)
                    .SetParameter("@respawnSeconds", item.RespawnTime.Seconds)
                    .SetParameter("@totalSpawnCount", item.TotalSpawnCount)
                    .SetParameter("@homeRange", item.HomeRange)
                    .SetParameter("@respawnMultiplierLow", item.RespawnMultiplierLow)
                    .SetParameter("@note", item.Note)
                    .ExecuteNonQuery();
            if ( res == 0 )
                throw new PerpetuumException(ErrorCodes.SQLUpdateError);
        }

        public void Delete(IFlockConfiguration item)
        {
            Db.Query().CommandText("delete npcflock where id=@ID")
                .SetParameter("@ID",item.ID)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }
    }
}