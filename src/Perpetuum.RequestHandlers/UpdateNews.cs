using Perpetuum.Data;
using Perpetuum.Host.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers
{
    public class UpdateNews : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var title = request.Data.GetOrDefault<string>(k.title);
                var body = request.Data.GetOrDefault<string>(k.body);
                var type = request.Data.GetOrDefault<int>(k.type);
                var language = request.Data.GetOrDefault<int>(k.language);
                var time = request.Data.GetOrDefault<DateTime>(k.time);
                var idx = request.Data.GetOrDefault<int>(k.ID);

                Db.Query().CommandText("UPDATE news SET [title]=@title, [body]=@body, [type]=@type, [language]=@language, [ntime]=@ntime WHERE [idx]=@idx")
                    .SetParameter("@title", title)
                    .SetParameter("@body", body)
                    .SetParameter("@type", type)
                    .SetParameter("@language", language)
                    .SetParameter("@ntime", time)
                    .SetParameter("@idx", idx)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);

                Message.Builder.FromRequest(request).WithOk().Send();

                scope.Complete();
            }
        }
    }
}
