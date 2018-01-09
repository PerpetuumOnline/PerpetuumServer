using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public class MissionArtifactReader: ArtifactReader
    {
        private readonly Guid _missionGuid;

        public MissionArtifactReader(Guid missionGuid)
        {
            _missionGuid = missionGuid;
        }

        public override IEnumerable<Artifact> GetArtifacts()
        {
            var x = Db.Query().CommandText("select * from artifacts where missionguid=@guid")
                .SetParameter("@guid", _missionGuid)
                .Execute()
                .Select(CreateArtifactFromRecord)
                .ToArray();

            return x;

        }
    }
}