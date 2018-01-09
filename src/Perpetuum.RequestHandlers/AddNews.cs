using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AddNews : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var title = request.Data.GetOrDefault<string>(k.title);
                var body = request.Data.GetOrDefault<string>(k.body);
                var type = request.Data.GetOrDefault<int>(k.type);
                var language = request.Data.GetOrDefault<int>(k.language);

                Db.Query().CommandText("insert into news (title,body,type,language) values (@title,@body,@type,@language)")
                    .SetParameter("@title", title)
                    .SetParameter("@body", body)
                    .SetParameter("@type", type)
                    .SetParameter("@language", language)
                    .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}