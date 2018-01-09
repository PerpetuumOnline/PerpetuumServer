using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations.YellowPages
{
    public class YellowPagesSearch : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var primaryActivity = request.Data.GetOrDefault(k.primaryActivity, -1);
            var primaryzone = request.Data.GetOrDefault(k.zoneID, -1);
            var primaryBase = request.Data.GetOrDefault(k.baseEID, (long)-1);
            var orientation = request.Data.GetOrDefault(k.orientation, -1);
            var lookingFor = request.Data.GetOrDefault(k.lookingFor, -1);
            var preferredFaction = request.Data.GetOrDefault(k.preferredFaction, -1);
            var providesInsurance = request.Data.GetOrDefault(k.providesInsurance, -1);
            var timeZone = request.Data.GetOrDefault(k.timeZone, -1);
            var requiredActivity = request.Data.GetOrDefault(k.requiredActivity, -1);
            var communication = request.Data.GetOrDefault(k.communication, -1);
            var services = request.Data.GetOrDefault(k.services, -1);


            var clauseList = new List<string>();
            var query = Db.Query();

            var searchString = "SELECT corporationeid FROM yellowpages WHERE ";

            if (primaryActivity > 0)
            {
                clauseList.Add("(primaryactivity & @primaryactivity) > 0 ");
                query.SetParameter("@primaryactivity", primaryActivity);
            }

            if (primaryzone >= 0)
            {
                clauseList.Add("zoneid = @primaryzone ");
                query.SetParameter("@primaryzone", primaryzone);
            }

            if (primaryBase > 0)
            {
                clauseList.Add("baseeid = @primarybase ");
                query.SetParameter("@primarybase", primaryBase);
            }

            if (orientation > 0)
            {
                clauseList.Add("(orientation & @orientation) > 0 ");
                query.SetParameter("@orientation", orientation);
            }

            if (lookingFor > 0)
            {
                clauseList.Add("(lookingfor & @lookingfor) > 0 ");
                query.SetParameter("@lookingfor", lookingFor);
            }

            if (preferredFaction > 0)
            {
                clauseList.Add("preferredfaction = @preferredfaction ");
                query.SetParameter("@preferredfaction", preferredFaction);
            }

            if (providesInsurance > 0)
            {
                clauseList.Add("(providesinsurance & @providesinsurance) > 0 ");
                query.SetParameter("@providesinsurance", providesInsurance);
            }

            if (timeZone > 0)
            {
                clauseList.Add("(timezone & @timezone) > 0 ");
                query.SetParameter("@timezone", timeZone);
            }

            if (requiredActivity > 0)
            {
                clauseList.Add("requiredactivity <= @requiredactivity ");
                query.SetParameter("@requiredactivity", requiredActivity);
            }

            if (communication > 0)
            {
                clauseList.Add("(communication & @communication) > 0 ");
                query.SetParameter("@communication", communication);
            }

            if (services > 0)
            {
                clauseList.Add("(services & @services) > 0");
                query.SetParameter("@services", services);
            }


            long[] corpEids;

            if (clauseList.Count == 0)
            {
                //all corps
                corpEids = query.CommandText("select corporationeid from yellowpages").Execute()
                    .Select(r => DataRecordExtensions.GetValue<long>(r, 0))
                    .ToArray();
            }
            else
            {
                searchString += clauseList.ArrayToString("AND ");

                query.CommandText(searchString);

                corpEids = query.Execute()
                    .Select(r => DataRecordExtensions.GetValue<long>(r, 0))
                    .ToArray();
            }

            if (corpEids.IsNullOrEmpty())
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
                return;
            }

            var result = new Dictionary<string, object>
            {
                {k.eid, corpEids}
            };

            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}