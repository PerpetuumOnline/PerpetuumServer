using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class RandomFlockReader : IRandomFlockReader
    {
        private ILookup<int, RandomFlockInfo> _flockInfos;

        public void Init()
        {
            _flockInfos = Db.Query().CommandText("select * from npcrandomflockpool")
                .Execute()
                .Select(r =>
                {
                    return new
                    {
                        presenceID = r.GetValue<int>("presenceid"),
                        info = new RandomFlockInfo
                        {
                            flockID = r.GetValue<int>("flockid"),
                            rate = r.GetValue<double>("rate"),
                            lastWave = r.GetValue<bool>("lastwave")
                        }
                    };
                }).ToLookup(x => x.presenceID, x => x.info);
        }

        public RandomFlockInfo[] GetByPresence(Presence presence)
        {
            return _flockInfos.GetOrEmpty(presence.Configuration.ID);
        }
    }
}