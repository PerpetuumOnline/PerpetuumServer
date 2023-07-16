using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZonePbsReplaceActualWithStaging : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                Db.Query()
                    .CommandText("UPDATE ed SET ed.descriptionstring = eds.descriptionstring FROM environmentdescription ed INNER JOIN environmentdescriptionstaging eds ON ed.definition = eds.definition")
                    .ExecuteNonQuery();

                Message.Builder.FromRequest(request).WithOk().Send();

                scope.Complete();
            }
        }
    }
}
