using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.GenXY;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class GetMyKillReports : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var fromOffset = request.Data.GetOrDefault<int>(k.@from);
            var toOffset = request.Data.GetOrDefault<int>(k.duration);
            var isVictim = request.Data.GetOrDefault<int>(k.victim) > 0;
            var isAttacker = request.Data.GetOrDefault<int>(k.attacker) > 0;
            var isKiller = request.Data.GetOrDefault<int>(k.killer) > 0;

            var dateFrom = DateTime.Now.AddDays(-fromOffset);
            var dateTo = dateFrom.AddDays(-toOffset);

            const string sqlCmd = @"select killreports.date,
		                                       killreports.data,
                                               characterkillreports.victim,
                                               characterkillreports.attacker,
                                               characterkillreports.killer,
                                               characterkillreports.reportid as guid

                                    from killreports INNER JOIN
                                    characterkillreports ON killreports.id = characterkillreports.reportid
                                    where characterid = @characterid and 
                                  date between @dateTo and @dateFrom and
                                  victim = @isVictim and (attacker = @isAttacker or killer = @isKiller)";

            var killReports = Db.Query().CommandText(sqlCmd)
                .SetParameter("@characterId",character.Id)
                .SetParameter("@dateTo",dateTo)
                .SetParameter("@dateFrom",dateFrom)
                .SetParameter("@isVictim",isVictim)
                .SetParameter("@isAttacker",isAttacker)
                .SetParameter("@isKiller",isKiller)
                .Execute()
                .RecordsToDictionary(valueConverter: kvp =>
                {
                    switch (kvp.Key)
                    {
                        case "data":
                        {
                            // data-t genxystring-re konvertaljuk,h
                            // a dataconverter jo helyre rakja
                            return GenxyString.FromObject(kvp.Value);
                        }
                        case "guid":
                        {
                            // reportid -> guid -> byte array
                            return ((Guid)kvp.Value).ToByteArray();
                        }
                    }

                    return kvp.Value;
                });

            var arguments = new Dictionary<string, object>
            {
                {k.characterID, character.Id},
                {k.victim, isVictim},
                {k.attacker, isAttacker},
                {k.killer, isKiller},
                {k.from, dateFrom },
                {k.to, dateTo}
            };

            var result = new Dictionary<string, object>
            {
                {k.data, killReports},
                {k.arguments, arguments}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}