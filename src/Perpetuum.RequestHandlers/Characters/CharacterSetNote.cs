using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterSetNote : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var target = Character.Get(request.Data.GetOrDefault<int>(k.target));
                var note = request.Data.GetOrDefault<string>(k.note);

                if (string.IsNullOrEmpty(note))
                {
                    // ha ures a string akkor toroljuk
                    Db.Query().CommandText("delete from characternotes where characterid = @characterid and targetid = @targetid")
                        .SetParameter("@characterid", character.Id)
                        .SetParameter("@targetid", target.Id).ExecuteNonQuery();
                    return;
                }

                note = note.Clamp(2000);
                var id = Db.Query().CommandText("select targetid from characternotes where characterid = @characterid and targetid = @targetid")
                    .SetParameter("@characterid", character.Id)
                    .SetParameter("@targetid", target.Id).ExecuteScalar<int>();

                if (id > 0)
                {
                    DynamicSqlQuery.Update("characternotes", new { note }, new { characterId = character.Id, targetId = target.Id }).ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
                }
                else
                {
                    DynamicSqlQuery.Insert("characternotes", new { characterId = character.Id, targetId = target.Id, note }).ThrowIfEqual(0, ErrorCodes.SQLInsertError);
                }

                Transaction.Current.OnCommited(() => Message.Builder.FromRequest(request).WithOk().Send());
                
                scope.Complete();
            }
        }
    }
}