using Perpetuum.Data;
using Perpetuum.Zones.NpcSystem.Flocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence
{
    public interface IEscalatingPresenceFlockSelector
    {
        IFlockConfiguration[] GetFlocksForPresenceLevel(int presenceId, int level);
        int GetMaxLevelForPresence(int presenceId);
    }
    public class EscalatingPresenceFlockSelector : IEscalatingPresenceFlockSelector
    {
        private readonly IFlockConfigurationRepository _flockConfigurationRepository;
        private readonly IEscalatingFlocksReader _reader;
        private readonly Random _random;
        public EscalatingPresenceFlockSelector(IEscalatingFlocksReader reader, IFlockConfigurationRepository flockConfigurationRepository)
        {
            _flockConfigurationRepository = flockConfigurationRepository;
            _reader = reader;
            _random = new Random();
        }
        public IFlockConfiguration[] GetFlocksForPresenceLevel(int presenceId, int level)
        {
            var infos = _reader.GetByPresence(presenceId).Where(info => info.Level == level);
            var flocks = new List<IFlockConfiguration>();
            foreach (var info in infos)
            {
                if (info.Chance >= _random.NextDouble())
                {
                    flocks.Add(_flockConfigurationRepository.Get(info.FlockId));
                }
            }
            return flocks.ToArray();
        }

        public int GetMaxLevelForPresence(int presenceId)
        {
            return _reader.GetByPresence(presenceId).Select(info=>info.Level).DefaultIfEmpty(0).Max();
        }
    }

    public class EscalationInfo
    {
        public int FlockId { get; set; }
        public int Level { get; set; }
        public double Chance { get; set; }
    }

    public interface IEscalatingFlocksReader
    {
        EscalationInfo[] GetByPresence(int presenceId);
    }

    public class EscalatingFlocksReader : IEscalatingFlocksReader
    {
        private ILookup<int, EscalationInfo> _flockInfos;

        public void Init()
        {
            _flockInfos = Db.Query().CommandText("select * from npcescalactions")
                .Execute()
                .Select(r =>
                {
                    return new
                    {
                        presenceID = r.GetValue<int>("presenceid"),
                        info = new EscalationInfo
                        {
                            FlockId = r.GetValue<int>("flockid"),
                            Level = r.GetValue<int>("level"),
                            Chance = r.GetValue<double>("chance")
                        }
                    };
                }).ToLookup(x => x.presenceID, x => x.info);
        }

        public EscalationInfo[] GetByPresence(int presenceId)
        {
            return _flockInfos.GetOrEmpty(presenceId);
        }
    }
}
