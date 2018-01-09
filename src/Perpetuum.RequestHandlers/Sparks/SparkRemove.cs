using System;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkRemove : IRequestHandler
    {
        private readonly SparkHelper _sparkHelper;

        public SparkRemove(SparkHelper sparkHelper)
        {
            _sparkHelper = sparkHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var deleteUnlocked = request.Data.GetOrDefault<int>(k.remove) == 1;

                _sparkHelper.GetActiveSparkId(character, out int sparkId, out DateTime? activationTime);

                if (sparkId > 0)
                {
                    _sparkHelper.DeactivateSpark(character, sparkId);
                }

                if (deleteUnlocked)
                {
                    Db.Query().CommandText("delete charactersparks where characterid=@characterID")
                        .SetParameter("@characterID", character.Id)
                        .ExecuteNonQuery();
                }

                _sparkHelper.SendSparksList(request);
                
                scope.Complete();
            }
        }
    }
}