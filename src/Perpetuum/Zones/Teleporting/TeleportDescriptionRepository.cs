using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.Teleporting
{
    public class TeleportDescriptionRepository : ITeleportDescriptionRepository
    {
        private readonly TeleportDescriptionBuilder.Factory _descriptionBuilderFactory;

        public TeleportDescriptionRepository(TeleportDescriptionBuilder.Factory descriptionBuilderFactory)
        {
            _descriptionBuilderFactory = descriptionBuilderFactory;
        }

        public void UpdateActive(TeleportDescription description)
        {
            Db.Query().CommandText("update teleportdescriptions set active=@active where id=@id")
                   .SetParameter("@id", description.id)
                   .SetParameter("@active", description.active)
                   .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public void Insert(TeleportDescription description)
        {
            const string insertCommand = @"INSERT dbo.teleportdescriptions (
	description,
	sourcecolumn,
	targetcolumn,
	sourcezone,
	sourcerange,
	targetzone,
	targetx,
	targety,
	targetz,
	targetrange,
	usetimeout,
	listable,
	type
) VALUES ( 
	@description,
	@sourcecolumn,
	@targetcolumn,
	@sourcezone,
	7,
	@targetzone,
	NULL,
	NULL,
	NULL,
	7,
	0,
	1,
	@type
) ";
            Db.Query().CommandText(insertCommand)
                .SetParameter("@description",description.description)
                .SetParameter("@sourcecolumn",description.SourceTeleport?.Eid)
                .SetParameter("@targetcolumn",description.TargetTeleport?.Eid)
                .SetParameter("@sourcezone",description.SourceZone?.Id)
                .SetParameter("@targetzone",description.TargetZone?.Id)
                .SetParameter("@type", (int)description.descriptionType)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public IEnumerable<TeleportDescription> GetAll()
        {
            return Db.Query().CommandText(@"select * from teleportdescriptions 
                                    where sourcezone in (select id from zones where enabled=1) and 
                                          targetzone in (select id from zones where enabled=1)")
                          .Execute()
                          .Select(CreateTeleportDescriptionFromRecord)
                          .ToArray();
        }

        private TeleportDescription CreateTeleportDescriptionFromRecord(IDataRecord record)
        {
            var builder = _descriptionBuilderFactory();
            builder.SetId(record.GetValue<int>("id"))
                .SetType((TeleportDescriptionType) record.GetValue<int>("type"))
                .SetDescription(record.GetValue<string>("description"))
                .SetSourceZone(record.GetValue<int?>("sourcezone") ?? -1)
                .SetSourceTeleport(record.GetValue<long?>("sourcecolumn") ?? 0L)
                .SetSourceRange(record.GetValue<int?>("sourcerange"))
                .SetTargetZone(record.GetValue<int?>("targetzone") ?? -1)
                .SetTargetTeleport(record.GetValue<long?>("targetcolumn") ?? 0L)
                .SetTargetRange(record.GetValue<int?>("targetrange") ?? 10)
                .UseTimeout(record.GetValue<int>("usetimeout"))
                .SetListable(record.GetValue<bool>("listable"))
                .SetActive(record.GetValue<bool>("active"));

            var targetX = record.GetValue<double?>("targetx");
            var targetY = record.GetValue<double?>("targety");
            if (targetX != null && targetY != null)
            {
                builder.SetLandingSpot(new Position((double)targetX, (double)targetY));
            }

            var td = builder.Build();
            return td;
        }
    }
}