using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public class TrainingZoneArtifactReader : ArtifactReader
    {
       

        public override IEnumerable<Artifact> GetArtifacts()
        {
            return Db.Query().CommandText("select * from trainingartifacts").Execute().Select(r =>
            {
                var artifactType = (ArtifactType)r.GetValue<int>("artifactType");
                var x = r.GetValue<int>("x");
                var y = r.GetValue<int>("y");

                var info = GetArtifactInfo(artifactType);

                return new Artifact(info, new Position(x, y), null);
            }).ToArray();
        }

       
    }
}