using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Log.Loggers;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.Players
{
    public class PlayerDeathLogger
    {
        public static readonly PlayerDeathLogger Log = new PlayerDeathLogger();

        private readonly ILogger<NpcDeathLogEvent> _logger;

        private PlayerDeathLogger()
        {
            _logger = new CompositeLogger<NpcDeathLogEvent>(
                new DelegateLogger<NpcDeathLogEvent>(WriteLogEventToDb),
                new DelegateLogger<NpcDeathLogEvent>(e =>
                    {
                        var killerInfo = string.Empty;
                        if (e.killer != null)
                        {
                            killerInfo = e.killer.InfoString;
                        }

                        Logger.Info($"Player dead. zone: {e.zone.Id} player: {e.player.InfoString} killer: {killerInfo}");
                    })
                );
        }

        private static void WriteLogEventToDb(NpcDeathLogEvent e)
        {
            Db.Query().CommandText("INSERT dbo.characternpcdeath ( characterid,npcdefinition,zoneid,x,y,playersrobot ) VALUES (@characterid,@npcdefinition,@zoneid,@x,@y,@playersRobot)")
                .SetParameter("@characterid", e.player.Character.Id)
                .SetParameter("@playersRobot", e.player.Definition)
                .SetParameter("@npcdefinition", e.killer.Definition)
                .SetParameter("@zoneid", e.zone.Id)
                .SetParameter("@x", e.player.CurrentPosition.intX)
                .SetParameter("@y", e.player.CurrentPosition.intY)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Write(IZone zone, Player player, Unit killer)
        {
            if (killer != null)
            {
                if (!(killer is Player))
                {
                    _logger.Log(new NpcDeathLogEvent
                    {
                        zone = zone,
                        player = player,
                        killer = killer
                    });
                }
            }

            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.PlayerDeath).SetCharacter(player.Character).SetItem(player);
            player.Character.LogTransaction(b);
        }

        private class NpcDeathLogEvent : ILogEvent
        {
            public IZone zone;
            public Player player;
            public Unit killer;
        }

        public static IDictionary<string, object> GetHistory(Character character, int from, int duration)
        {
            var dateFrom = DateTime.Now.AddDays(-from);
            var dateTo = dateFrom.AddDays(-duration);

            const string query = "select * from characternpcdeath where characterid=@characterId and eventtime between @dateTo and @dateFrom";

            return Db.Query().CommandText(query)
                .SetParameter("@characterId", character.Id)
                .SetParameter("@dateFrom", dateFrom)
                .SetParameter("@dateTo", dateTo)
                .Execute()
                .ToDictionary("n", r => new Dictionary<string, object>
                {
                    {"date", ((IDataRecord) r).GetValue<DateTime>("eventtime")},
                    {"robot",((IDataRecord) r).GetValue<int>("playersrobot")},
                    {"definition", ((IDataRecord) r).GetValue<int>("npcdefinition")},
                    {"zoneID",((IDataRecord) r).GetValue<int>("zoneID")},
                    {"x",((IDataRecord) r).GetValue<int>("x")},
                    {"y",((IDataRecord) r).GetValue<int>("y")}
                });
        }
    }
}