using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;

namespace Perpetuum.Groups.Alliances
{
    public class PrivateAlliance : Alliance
    {
        public new static PrivateAlliance GetOrThrow(long eid)
        {
            return (PrivateAlliance) Repository.LoadOrThrow(eid);
        }

        public static PrivateAlliance Create(AllianceDescription description)
        {
            var systemStorage = SystemContainer.GetByName(k.es_private_alliance);
            var newAlliance = Create(EntityDefault.GetByName(DefinitionNames.PRIVATE_ALLIANCE), systemStorage, description, EntityIDGenerator.Random);
            return (PrivateAlliance) newAlliance;
        }
    }

    public class DefaultAlliance : Alliance
    {
    }
  

    public abstract class Alliance : Entity
    {
        private Corporation[] _corporations;
        
        public IEnumerable<Character> GetCharacterMembers()
        {
            return Corporations.SelectMany(c => c.GetCharacterMembers());
        }

        public IEnumerable<Corporation> Corporations
        {
            get { return _corporations ?? (_corporations = LoadCorporationsFromDb()); }
        }

        private Corporation[] LoadCorporationsFromDb()
        {
            return Db.Query().CommandText("select corporationEID from alliancemembers where allianceEID=@allianceEID")
                           .SetParameter("@allianceEID", Eid)
                           .Execute()
                           .Select(r => Corporation.GetOrThrow(r.GetValue<long>(0))).ToArray();
        }

        
        public bool IsCorporationMember(Corporation corporation)
        {
            return Corporations.Contains(corporation);
        }

     
        [CanBeNull]
        public static Alliance GetByCorporation(Corporation corporation)
        {
            var eid = Db.Query().CommandText("select allianceEID from allianceMembers where corporationEID=@corpEID")
                              .SetParameter("@corpEID", corporation.Eid)
                              .ExecuteScalar<long>();

            if (eid == 0L)
                return null;

            return GetOrThrow(eid);
        }

      
        public static Alliance GetOrThrow(long eid)
        {
            return (Alliance)Repository.LoadOrThrow(eid);
        }

        protected static Alliance Create(EntityDefault entityDefault,SystemContainer container, AllianceDescription allianceDescription, EntityIDGenerator generator)
        {
            var alliance = Factory.Create(entityDefault,generator);
            alliance.Parent = container.Eid;
            Repository.Insert(alliance);

            Db.Query().CommandText("insert into alliances (allianceEID, name, nick, defaultAlliance) values (@eid, @name, @nick, @defaultAlliance)")
                .SetParameter("@eid", alliance.Eid)
                .SetParameter("@name", allianceDescription.name)
                .SetParameter("@nick", allianceDescription.nick)
                .SetParameter("@defaultAlliance", allianceDescription.isDefault)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLInsertError);

            return (Alliance) alliance;
        }

        public IEnumerable<Character> Members
        {
            get { return GetCharacterMembers(); }
        }

        public bool IsMember(Character character)
        {
            return Members.Any(m => m == character);
        }
    }
}
