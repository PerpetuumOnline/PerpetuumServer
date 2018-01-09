using System.Linq;
using System.Transactions;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks;
using Perpetuum.Services.Standing;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkUnlock : IRequestHandler
    {
        private readonly SparkHelper _sparkHelper;
        private readonly IStandingHandler _standingHandler;

        public SparkUnlock(SparkHelper sparkHelper,IStandingHandler standingHandler)
        {
            _sparkHelper = sparkHelper;
            _standingHandler = standingHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var sparkId = request.Data.GetOrDefault<int>(k.sparkID);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var spark = _sparkHelper.GetSpark(sparkId).ThrowIfNull(ErrorCodes.ItemNotFound);
                spark.defaultSpark.ThrowIfTrue(ErrorCodes.SparkAlreadyUnlocked);
                spark.unlockable.ThrowIfFalse(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                _sparkHelper.GetUnlockedSparkData(character).Any(uls=>uls.sparkId == sparkId).ThrowIfTrue(ErrorCodes.SparkAlreadyUnlocked);

                if (spark.unlockPrice != null)
                {
                    character.SubtractFromWallet(TransactionType.SparkUnlock,(double) spark.unlockPrice);
                }

                if (spark.definition != null)
                {
                    var publicContainer = character.GetPublicContainerWithItems();

                    //keress meg itemet
                    var foundItems = publicContainer.GetItems().Where(i => i.Definition == spark.definition);
                
                    var needed = (int) spark.quantity;

                    foreach (var item in foundItems)
                    {
                        if (item.Quantity > needed)
                        {
                            item.Quantity = item.Quantity - needed;
                            needed = 0;
                            break; //found more
                        }
                    
                        if (item.Quantity <= needed )
                        {
                            needed = needed - item.Quantity;

                            Entity.Repository.Delete(item);

                            if (needed <= 0)
                            {
                                break; //found enough
                            }
                        }
                    }

                    needed.ThrowIfGreater(0, ErrorCodes.SparkNotEnoughItems);
                    publicContainer.Save();
                }

                if (spark.energyCredit != null)
                {
                    //%%% itt a cucc    
                }

                if (spark.allianceEid != null)
                {
                    var standing = _standingHandler.GetStanding((long) spark.allianceEid, character.Eid);
                    standing.ThrowIfLess((double)spark.standingLimit, ErrorCodes.StandingTooLow);
                }

                _sparkHelper.UnlockSpark(character, sparkId);

                //return list
                Transaction.Current.OnCommited(() => _sparkHelper.SendSparksList(request));
                
                scope.Complete();
            }
        }
    }
}