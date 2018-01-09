using System.Collections.Generic;
using System.Linq;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;

namespace Perpetuum.Groups.Corporations
{
    partial class Corporation
    {
        [NotNull]
        public static Corporation GetOrThrow(long eid)
        {
            return Get(eid).ThrowIfNull(ErrorCodes.CorporationNotExists);
        }

        [CanBeNull]
        public static Corporation Get(long eid)
        {
            return (Corporation)Repository.Load(eid);
        }

        protected static Corporation Create(EntityDefault entityDefault, SystemContainer container, CorporationDescription corporationDescription, EntityIDGenerator generator)
        {
            var corporation = Factory.Create(entityDefault,generator);
            corporation.Parent = container.Eid;
            corporation.Save();

            const string insertCommandText = @"insert into corporations (eid, name, nick, wallet, taxrate, publicProfile, privateProfile,defaultcorp, founder) 
                                                                 values (@eid, @name, @nick, @wallet, @taxrate, @publicProfile, @privateProfile,@defaultcorp,@founder)";
            Db.Query().CommandText(insertCommandText)
                .SetParameter("@eid", corporation.Eid)
                .SetParameter("@name", corporationDescription.name)
                .SetParameter("@nick", corporationDescription.nick)
                .SetParameter("@wallet", 0)
                .SetParameter("@taxrate", corporationDescription.taxRate)
                .SetParameter("@publicProfile", GenxyConverter.Serialize((Dictionary<string, object>)corporationDescription.publicProfile))
                .SetParameter("@privateProfile", GenxyConverter.Serialize((Dictionary<string, object>)corporationDescription.privateProfile))
                .SetParameter("@defaultCorp", corporationDescription.isDefault).SetParameter("@founder", corporationDescription.founder)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            return (Corporation)corporation;
        }

        public int GetMaximumProbeAmount()
        {
            const int maxProbesExtensionId = 330;
            return  CEO.GetExtensionLevel(maxProbesExtensionId);
        }

        public IEnumerable<long> GetProximityProbeEids()
        {
            var probeDefinitions = EntityServices.Defaults.GetAll().GetDefinitionsByCategoryFlag(CategoryFlags.cf_proximity_probes);
            var queryStr = $"SELECT eid FROM entities WHERE owner=@corporationEID and definition in ({probeDefinitions.ArrayToString()})";

            return  Db.Query().CommandText(queryStr).SetParameter("@corporationEID",Eid)
                .Execute()
                .Select(r => r.GetValue<long>(0))
                .ToArray();
        }
    }
}
