using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterNickHistory : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var characterId = request.Data.GetOrDefault<int>(k.characterID);

            var counter = 0;
            var entries = new Dictionary<string, object>();
            var records =
            Db.Query().CommandText("select * from characternickhistory where characterid=@characterID")
                    .SetParameter("@characterID", characterId)
                    .Execute();

            foreach (var r in records)
            {
                var entry = new Dictionary<string, object>()
                    {
                        {k.nick, r.GetValue<string>("nick")},
                        {k.date, r.GetValue<DateTime>("eventdate")}
                    };

                entries.Add("c" + counter ++, entry);

            }

            var result = new Dictionary<string, object>()
                {
                    {k.characterID, characterId},
                    {k.aliases, entries},
                };

            Message.Builder.FromRequest(request).WithData(result).Send();


        }
    }
}
