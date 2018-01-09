using System;
using Perpetuum.Data;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public class ZoneArtifactRepository : ArtifactRepository
    {
        private readonly IZone _zone;

        public ZoneArtifactRepository(IZone zone,IArtifactReader reader) : base(reader)
        {
            _zone = zone;
        }

        public override void InsertArtifact(Artifact artifact)
        {
            const string sqlCommandText = @"insert into artifacts (artifacttype,characterid,zoneid,positionx,positiony,missionguid)
                                                                  values
                                                                  (@artifactType,@characterId,@zoneId,@positionX,@positionY,@missionGuid)";

            Db.Query().CommandText(sqlCommandText)
                .SetParameter("@artifactType",artifact.Info.type)
                .SetParameter("@characterId", artifact.Character?.Id ?? 0)
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@positionX",artifact.Position.intX)
                .SetParameter("@positionY",artifact.Position.intY)
                .SetParameter("@missionGuid", artifact.MissionGuid == Guid.Empty ? (object)null : artifact.MissionGuid)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }

        public override void DeleteArtifact(Artifact artifact)
        {
            Db.Query().CommandText("delete from artifacts where id = @artifactId")
                .SetParameter("@artifactId",artifact.Id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        
    }
}