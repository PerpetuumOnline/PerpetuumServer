using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class PollGet : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var pollID = Db.Query().CommandText("select min(pollid) from polls where active=1 and pollid not in (select pollid from pollanswers where accountid=@accountID)")
                .SetParameter("@accountID", request.Session.AccountId)
                .ExecuteScalar<int>();

            if (pollID == 0)
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
                return;
            }

            var onePoll = Db.Query().CommandText("select pollid,topic,started from polls where pollid=@pollID")
                .SetParameter("@pollID", pollID)
                .ExecuteSingleRow();

            var poll = new Dictionary<string, object>
            {
                {k.ID, onePoll.GetValue<int>(0)},
                {k.topic, onePoll.GetValue<string>(1)},
                {k.started, onePoll.GetValue<DateTime>(2)}
            };

            var choices = Db.Query().CommandText("select choiceid,choicetext from pollchoices where pollid=@pollID")
                .SetParameter("@pollID", pollID)
                .Execute()
                .ToDictionary("c", r => new Dictionary<string, object>
                {
                    {k.choiceID, DataRecordExtensions.GetValue<int>(r, 0)},
                    {k.choiceText, DataRecordExtensions.GetValue<string>(r, 1)}
                });

            if (choices.Count > 0)
            {
                poll.Add(k.choices, choices);
                Message.Builder.FromRequest(request).WithData(poll).WrapToResult().Send();
            }
            else
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
        }
    }
}