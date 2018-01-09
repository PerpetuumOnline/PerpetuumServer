using System;
using System.Linq;
using System.Transactions;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkChange : IRequestHandler
    {
        private readonly SparkHelper _sparkHelper;

        public SparkChange(SparkHelper sparkHelper)
        {
            _sparkHelper = sparkHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sparkId = request.Data.GetOrDefault<int>(k.sparkID);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var spark = _sparkHelper.GetSpark(sparkId).ThrowIfNull(ErrorCodes.ItemNotFound);

                if (spark.changePrice > 0)
                {
                    character.SubtractFromWallet(TransactionType.SparkActivation,spark.changePrice);
                }

                var unlockedSparks = _sparkHelper.GetUnlockedSparkData(character).ToArray();

                if (!spark.defaultSpark)
                {
                    //is spark unlocked?
                    unlockedSparks.Any(uls => uls.sparkId == sparkId).ThrowIfFalse(ErrorCodes.SparkLocked);
                }

                var currentSpark = unlockedSparks.FirstOrDefault(uls => uls.active);

                if (currentSpark != null)
                {
                    (currentSpark.sparkId == sparkId).ThrowIfTrue(ErrorCodes.SparkAlreadyActive);

                    if (currentSpark.activationTime != null)
                    {
                        (DateTime.Now.Subtract((DateTime) currentSpark.activationTime).TotalMinutes < SparkHelper.SPARK_CHANGE_MINUTES).ThrowIfTrue(ErrorCodes.SparkCooldownNotOver);
                    }

                    if (currentSpark.sparkId > 0)
                    {
                        _sparkHelper.DeactivateSpark(character, currentSpark.sparkId);
                    }
                }

                _sparkHelper.ActivateSpark(character, sparkId);

                //return list
                Transaction.Current.OnCommited(() => _sparkHelper.SendSparksList(request));
                
                scope.Complete();
            }
        }
    }
}