using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class UseLotteryItem : IRequestHandler
    {
        private readonly IEntityServices _entityServices;
		private readonly IAccountManager accountManager;

        public UseLotteryItem(IEntityServices entityServices, IAccountManager accountManager)
        {
            _entityServices = entityServices;
			this.accountManager = accountManager;
        }


        public void HandleRequest(IRequest request)
        {
            var itemEid = request.Data.GetOrDefault<long>(k.itemEID);
            var item = _entityServices.Repository.Load(itemEid);
            if (item is LotteryItem)
                HandleLottery(request);
			else if (item is EPBoost)
				HandleEPBoost(request, itemEid);
            else if (item is Paint) //TODO this is here until we can build a good category flag..
                HandlePaint(request, itemEid);
            else if (item is CalibrationProgramCapsule)
                HandleCalibrationTemplateItem(request, itemEid);
        }

		private void HandleEPBoost(IRequest request, long itemEid)
		{
			using (var scope = Db.CreateTransaction())
			{
				var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
				var character = request.Session.Character;
				var account = accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

				var container = Container.GetWithItems(containerEid, character);

				var containerItem = (EPBoost)container.GetItemOrThrow(itemEid, true).Unstack(1);

				containerItem.Activate(accountManager, account);

				_entityServices.Repository.Delete(containerItem);
				container.Save();
				LogActivation(character, container, containerItem);
				

				Transaction.Current.OnCommited(() =>
				{
					//Send custom message back in "Redeemables" dialog
					var boostDict = containerItem.ToDictionary();
					boostDict[k.quantity] = -1;  //Indicate the consumption of item from stack
					var result = new Dictionary<string, object>
					{
						{ k.container, container.ToDictionary() },
						{ k.item, boostDict }
					};
					Message.Builder.FromRequest(request).WithData(result).Send();
				});

				scope.Complete();
			}
		}

        private void HandleCalibrationTemplateItem(IRequest request, long itemEid)
        {
            using (var scope = Db.CreateTransaction())
            {
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var container = Container.GetWithItems(containerEid, character);
                container.ThrowIfNotType<PublicContainer>(ErrorCodes.ContainerHasToBeOnADockingBase); //Error for unpacking elsewhere

                var ctCapsule = (CalibrationProgramCapsule)container.GetItemOrThrow(itemEid, true).Unstack(1);
                var ctDef = ctCapsule.Activate();

                var ctItem = (Item)_entityServices.Factory.CreateWithRandomEID(ctDef);
                ctItem.Owner = character.Eid;

                container.AddItem(ctItem, false); // CTs dont stack
                _entityServices.Repository.Delete(ctCapsule);
                container.Save();

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        { k.container, container.ToDictionary() },
                        { k.item, ctItem.ToDictionary() }
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

        public void HandleLottery(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var itemEid = request.Data.GetOrDefault<long>(k.itemEID);
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var character = request.Session.Character;

                var container = Container.GetWithItems(containerEid, character);
                var lotteryItem = (LotteryItem)container.GetItemOrThrow(itemEid, true).Unstack(1);

                var randomEd = lotteryItem.PickRandomItem();
                var randomItem = (Item)_entityServices.Factory.CreateWithRandomEID(randomEd);
                randomItem.Owner = character.Eid;

                container.AddItem(randomItem, true);
                _entityServices.Repository.Delete(lotteryItem);
                container.Save();

                LogOpen(character, container, lotteryItem);
                LogRandomItemCreated(character, container, randomItem);

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.container,container.ToDictionary()},
                        {k.item,randomItem.ToDictionary()}
                    };

                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }

        private static void LogOpen(Character character, Container container, LotteryItem lotteryItem)
        {
            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.LotteryOpen)
                                                 .SetCharacter(character)
                                                 .SetContainer(container)
                                                 .SetItem(lotteryItem);
            character.LogTransaction(b);
        }

		private static void LogActivation(Character character, Container container, Item item)
		{
			var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.ItemRedeem)
												 .SetCharacter(character)
												 .SetContainer(container)
												 .SetItem(item);
			character.LogTransaction(b);
		}

        private static void LogRandomItemCreated(Character character, Container container, Item randomItem)
        {
            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.LotteryRandomItemCreated)
                                                 .SetCharacter(character)
                                                 .SetContainer(container)
                                                 .SetItem(randomItem);
            character.LogTransaction(b);
        }



        private void HandlePaint(IRequest request, long paintEid)
        {
            using (var scope = Db.CreateTransaction())
            {
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var container = Container.GetWithItems(containerEid, character);
                container.ThrowIfNotType<RobotInventory>(ErrorCodes.RobotMustBeSelected); //TODO better error to indicate item not being activated in robot cargo

                var paintItem = (Paint)container.GetItemOrThrow(paintEid, true).Unstack(1);
                paintItem.Activate(container as RobotInventory, character);
                _entityServices.Repository.Delete(paintItem);
                container.Save();

                Transaction.Current.OnCommited(() =>
                {
                    //Send custom message back in "Redeemables" dialog
                    var paintDict = paintItem.ToDictionary();
                    paintDict[k.quantity] = -1;  //Indicate the consumption of item from stack
                    var result = new Dictionary<string, object>
                    {
                        { k.container, container.ToDictionary() },
                        { k.item, paintDict}
                    };
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });

                scope.Complete();
            }
        }
    }
}