using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionRevert : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var extensionId = request.Data.GetOrDefault<int>(k.ID);
                var fee = request.Data.GetOrDefault<int>(k.fee);

                Logger.Info("reverting extension:" + extensionId + " fee: " + fee);
                var characterIDs = Db.Query().CommandText("SELECT distinct characterid FROM accountextensionspent WHERE extensionid=@extensionID")
                    .SetParameter("@extensionID", extensionId)
                    .Execute()
                    .Select(r => DataRecordExtensions.GetValue<int>(r, 0)).ToList();

                foreach (var characterId in characterIDs)
                {
                    var res = Db.Query().CommandText("update characters set credit=credit+@fee where characterid=@characterId").SetParameter("@characterId", characterId).SetParameter("@fee", fee)
                        .ExecuteNonQuery();

                    if (res != 1)
                    {
                        Logger.Error("character was not found: " + characterId);
                        continue;
                    }

                    Logger.Info("characterID:" + characterId + " extensionID:" + extensionId + " returned:" + fee);
                }

                //delete skill and EP cost (return ep)
                Db.Query().CommandText("DELETE characterextensions WHERE extensionid=@extensionID; DELETE accountextensionspent WHERE extensionid=@extensionID")
                    .SetParameter("@extensionID", extensionId)
                    .ExecuteNonQuery();

                Logger.Info("extension reverted. " + extensionId);
                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}