using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Players;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public class ZoneArtifactReader : ArtifactReader
    {
        private readonly Player _player;
        private readonly ArtifactReadMode _readMode;

        public ZoneArtifactReader(Player player, ArtifactReadMode readMode = ArtifactReadMode.All)
        {
            _player = player;
            _readMode = readMode;
        }

        public override IEnumerable<Artifact> GetArtifacts()
        {
            var zone = _player.Zone;
            if (zone == null) return new Artifact[0];

            var artifacts = Db.Query().CommandText("select * from artifacts where characterid = @characterId and zoneid = @zoneId")
                .SetParameter("@characterId", _player.Character.Id)
                .SetParameter("@zoneId", zone.Id)
                .Execute()
                .Select(CreateArtifactFromRecord);

            var resultList = new List<Artifact>();

            foreach (var artifact in artifacts)
            {
                var add = true;

                switch (_readMode)
                {
                    case ArtifactReadMode.Persistent:
                        if (!artifact.Info.isPersistent)
                        {
                            add = false;
                        }
                        break;

                    case ArtifactReadMode.NonPersistent:
                        if (artifact.Info.isPersistent)
                        {
                            add = false;
                        }
                        break;
                }

                if (add)
                {
                    resultList.Add(artifact);
                }
            }


            return resultList;
        }



    }
}