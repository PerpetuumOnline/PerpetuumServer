using System;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class PollAnswer : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var pollID = request.Data.GetOrDefault<int>(k.ID);
                var answer = request.Data.GetOrDefault<int>(k.answer);

                var record = Db.Query().CommandText("select participation,active from polls where pollid=@pollID")
                    .SetParameter("@pollID", pollID)
                    .ExecuteSingleRow().ThrowIfNull(ErrorCodes.ItemNotFound);

                var participation = record.GetValue<int>(0);
                var active = record.GetValue<bool>(1);

                active.ThrowIfFalse(ErrorCodes.PollClosed);

                Db.Query().CommandText("insert pollanswers (pollid,accountid,answerid) values (@pollID,@accountID,@answerID)")
                    .SetParameter("@pollID", pollID)
                    .SetParameter("@accountID", request.Session.AccountId)
                    .SetParameter("@answerID", answer)
                    .ExecuteNonQuery().ThrowIfNotEqual(1, ErrorCodes.SQLInsertError);

                var nofAnswers = Db.Query().CommandText("select count(*) from pollanswers where pollid=@pollID")
                    .SetParameter("@pollID", pollID)
                    .ExecuteScalar<int>();

                //is participation level reached?
                if (nofAnswers >= participation)
                {
                    Db.Query().CommandText("update polls set active=0,ended=@now where pollid=@pollID")
                        .SetParameter("@pollID", pollID)
                        .SetParameter("@now", DateTime.Now)
                        .ExecuteNonQuery().ThrowIfNotEqual(1, ErrorCodes.SQLUpdateError);
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}