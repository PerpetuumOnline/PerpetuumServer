using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.ProductionEngine
{
    public static class ProductionInProgressExtensions
    {
        public static IDictionary<string, object> ToDictionary(this IEnumerable<ProductionInProgress> productions)
        {
            return productions.ToDictionary("c", p => p.ToDictionary());
        }
        
        public static IEnumerable<ProductionInProgress> GetByCorporation(this IEnumerable<ProductionInProgress> productions,Corporation corporation)
        {
            return corporation.GetCharacterMembers().SelectMany(productions.GetCorporationPaidProductionsByCharacter);
        }

        public static IEnumerable<ProductionInProgress> GetCorporationPaidProductionsByFacililtyAndCharacter(this IEnumerable<ProductionInProgress> productions,Character character, long facilityEid)
        {
            return productions.GetRunningProductionsByFacilityAndCharacter(character, facilityEid).Where(pip => pip.useCorporationWallet);
        }

        public static IEnumerable<ProductionInProgress> GetRunningProductionsByFacilityAndCharacter(this IEnumerable<ProductionInProgress> productions, Character character, long facilityEid)
        {
            return productions.GetByCharacter(character).Where(pip => pip.facilityEID == facilityEid);
        }

        public static IEnumerable<ProductionInProgress> GetCorporationPaidProductionsByCharacter(this IEnumerable<ProductionInProgress> productions,Character character)
        {
            return productions.GetByCharacter(character).Where(pip => pip.useCorporationWallet);
        }
        
        public static IEnumerable<ProductionInProgress> GetByCharacter(this IEnumerable<ProductionInProgress> productions, Character character)
        {
            return productions.Where(pip => pip.character == character);
        }
    }

    public interface IProductionInProgressRepository
    {
        void Delete(ProductionInProgress productionInProgress);

        IEnumerable<ProductionInProgress> GetAllByFacility(ProductionFacility facility);
        IEnumerable<ProductionInProgress> GetAll();
    }

    public class ProductionInProgressRepository : IProductionInProgressRepository
    {
        private readonly ProductionInProgress.Factory _pipFactory;

        public ProductionInProgressRepository(ProductionInProgress.Factory pipFactory)
        {
            _pipFactory = pipFactory;
        }

        public void Delete(ProductionInProgress pip)
        {
            Db.Query().CommandText("delete runningproductionreserveditem where runningid=@ID")
                .SetParameter("@ID",pip.ID)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);

            Db.Query().CommandText("delete runningproduction where ID=@ID")
                .SetParameter("@ID", pip.ID)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        public IEnumerable<ProductionInProgress> GetAllByFacility(ProductionFacility facility)
        {
            return Db.Query().CommandText("select * from runningproduction where facilityEID = @facilityEID")
                           .SetParameter("@facilityEID",facility.Eid)
                           .Execute()
                           .Select(CreateProductionInProgressFromRecord)
                           .ToArray();
        }

        public IEnumerable<ProductionInProgress> GetAll()
        {
            return Db.Query().CommandText("select * from runningproduction").Execute().Select(CreateProductionInProgressFromRecord).ToArray();
        }

        private ProductionInProgress CreateProductionInProgressFromRecord(IDataRecord record)
        {
            var pip = _pipFactory();
            pip.ID = record.GetValue<int>("id");
            pip.character = Character.Get(record.GetValue<int>("characterID"));
            pip.resultDefinition = record.GetValue<int>("resultDefinition");
            pip.type = (ProductionInProgressType) record.GetValue<int>("type");
            pip.startTime = record.GetValue<DateTime>("startTime");
            pip.finishTime = record.GetValue<DateTime>("finishTime");
            pip.facilityEID = record.GetValue<long>("facilityEID");
            pip.totalProductionTimeSeconds = record.GetValue<int>("totalProductionTime");
            pip.baseEID = record.GetValue<long>("baseEID");
            pip.creditTaken = record.GetValue<double>("creditTaken");
            pip.pricePerSecond = record.GetValue<double>("pricePerSecond");
            pip.useCorporationWallet = record.GetValue<bool>("useCorporationWallet");
            pip.amountOfCycles = record.GetValue<int>("amountOfCycles");
            pip.paused = record.GetValue<bool>("paused");
            pip.pauseTime = record.GetValue<DateTime?>("pausetime");
            return pip;
        }
    }

    public class ProductionInProgress
    {
        private readonly ItemHelper _itemHelper;
        private readonly DockingBaseHelper _dockingBaseHelper;
        public int ID;
        public Character character;
        public int resultDefinition;
        public ProductionInProgressType type;
        public DateTime startTime;
        public DateTime finishTime;
        public long facilityEID;
        public int totalProductionTimeSeconds;
        public long baseEID;
        public double creditTaken;
        public double pricePerSecond;
        public int amountOfCycles;
        public bool useCorporationWallet;
        public bool paused;
        public DateTime? pauseTime;

        public long[] ReservedEids;

        public TimeSpan TotalProductionTime => TimeSpan.FromSeconds(totalProductionTimeSeconds);

        public delegate ProductionInProgress Factory();

        public ProductionInProgress(ItemHelper itemHelper,DockingBaseHelper dockingBaseHelper)
        {
            _itemHelper = itemHelper;
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void WriteLog()
        {
            ProductionHelper.ProductionLogInsert(character, resultDefinition, GetResultingAmount(), type, CalculateDurationSeconds(), Price, useCorporationWallet);
        }

        public IEnumerable<Item> GetReservedItems()
        {
            foreach (var reservedEid in ReservedEids)
            {
                var item = _itemHelper.LoadItem(reservedEid);
                if (item != null)
                    yield return item;
            }
        }

        public void LoadReservedItems()
        {
            ReservedEids =
                (from r in
                     Db.Query().CommandText("select reservedEID from runningproductionreserveditem where runningID=@ID").SetParameter("@ID", ID)
                     .Execute()
                 select r.GetValue<long>(0)).ToArray();
        }

        private int CalculateDurationSeconds()
        {
            return (int)finishTime.Subtract(startTime).TotalSeconds;
        }


        private int GetResultingAmount()
        {

            EntityDefault ed;
            if (!EntityDefault.TryGet(resultDefinition, out ed))
            {
                Logger.Error("consistency error! definition was not found for productioninprogress withdrawcredit. definition: " + resultDefinition);
            }

            var resultingAmount = amountOfCycles * ed.Quantity;

            return resultingAmount;
        }


        private double Price
        {
            get
            {
                if (IsMissionRelated)
                {
                    return 0;
                }

                switch (type)
                {

                    case ProductionInProgressType.massProduction:
                        return totalProductionTimeSeconds  * pricePerSecond;

                    default:
                        return totalProductionTimeSeconds  * pricePerSecond;
                }

            }
        }

        public bool IsMissionRelated
        {
            get
            {
                var resultEd = EntityDefault.Get(resultDefinition);

                if (resultEd.CategoryFlags.IsCategory(CategoryFlags.cf_random_items) ||
                    resultEd.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs))
                {
                    return true;
                }

                return false;
            }
        }


        public Dictionary<string, object> ToDictionary()
        {
            var resultQuantity = amountOfCycles;
            EntityDefault resultEd;
            if (EntityDefault.TryGet(resultDefinition, out resultEd))
            {
                resultQuantity *= resultEd.Quantity;
            }

            var tmpDict = new Dictionary<string, object>
                              {
                                  {k.definition, resultDefinition},
                                  {k.startTime, startTime},
                                  {k.finishTime, finishTime},
                                  {k.ID, ID},
                                  {k.facility, facilityEID},
                                  {k.productionTime, totalProductionTimeSeconds},
                                  {k.timeLeft, (int) finishTime.Subtract(DateTime.Now).TotalSeconds},
                                  {k.type, (int) type},
                                  {k.baseEID, baseEID},
                                  {k.cycle, resultQuantity},
                                  {k.price, Price},
                                  {k.characterID, character.Id},
                                  {k.useCorporationWallet, useCorporationWallet},
                                  {k.paused, paused},
                                  {k.pauseTime, pauseTime},
                              };

            return tmpDict;

        }

        public override string ToString()
        {
            return string.Format("ID:{0} characterID:{1} characterEID:{2} resultDefinition:{3} {9} type:{4} facilityEID:{5} baseEID:{6} price:{7} amountOfCycles:{8}", ID, character.Id, character.Eid, resultDefinition, type, facilityEID, baseEID, Price, amountOfCycles, EntityDefault.Get(resultDefinition).Name);
        }



        /// <summary>
        /// take credit for the production from the character
        /// </summary>
        public bool TryWithdrawCredit()
        {
            Logger.Info("withdrawing credit for: " + this);

            var transactionType = TransactionType.ProductionManufacture;
            switch (type)
            {
                case ProductionInProgressType.licenseCreate:
                case ProductionInProgressType.manufacture:
                case ProductionInProgressType.patentMaterialEfficiencyDevelop:
                case ProductionInProgressType.patentNofRunsDevelop:
                case ProductionInProgressType.patentTimeEfficiencyDevelop:
                {
                    Logger.Error("consistency error! outdated production type. " + type);
                    throw new PerpetuumException(ErrorCodes.ServerError);
                }

                case ProductionInProgressType.research:
                    transactionType = TransactionType.ProductionResearch;
                    break;
                case ProductionInProgressType.prototype:
                    transactionType = TransactionType.ProductionPrototype;
                    break;
                case ProductionInProgressType.massProduction:
                    transactionType = TransactionType.ProductionMassProduction;
                    break;

                case ProductionInProgressType.calibrationProgramForge:
                    transactionType = TransactionType.ProductionCPRGForge;
                    break;
            }

            //no price, no process
            if (Math.Abs(Price) < double.Epsilon)
            {
                Logger.Info("price is 0 for " + this);
                return true;
            }

            //take his money
            creditTaken = Price; //safety for the cancel
            
            var wallet = character.GetWallet(useCorporationWallet,transactionType);

            if (wallet.Balance < Price)
                return false;

            wallet.Balance -= Price;

            var b = TransactionLogEvent.Builder()
                                       .SetTransactionType(transactionType)
                                       .SetCreditBalance(wallet.Balance)
                                       .SetCreditChange(-Price)
                                       .SetCharacter(character)
                                       .SetItem(resultDefinition, GetResultingAmount());
            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                character.LogTransaction(b);
            }

            _dockingBaseHelper.GetDockingBase(baseEID).AddCentralBank(transactionType, Price);
            return true;
        }

        public void SetPause(bool isPaused)
        {
            if (isPaused)
            {
                if (!paused)
                {
                    // >> to paused state
                    PauseProduction();
                }
            }
            else
            {
                if (paused)
                {
                    // >> to unpause state
                    ResumeProduction();
                    
                }
            }
        }

        private void ResumeProduction()
        {
            if (!paused)
            {
                Logger.Warning("production already running: " + this);
                return;
            }

            paused = false;

            if (pauseTime == null)
            {
                Logger.Error("wtf pause time is null. " + this);
                return;
            }

            var pauseMoment = (DateTime) pauseTime;

            var doneBackThen = pauseMoment.Subtract(startTime);

            startTime = DateTime.Now.Subtract(doneBackThen);
            finishTime = startTime.AddSeconds(totalProductionTimeSeconds);

            var res =
                Db.Query().CommandText("update runningproduction set starttime=@startTime,finishtime=@finishTime,paused=0,pauseTime=null where id=@id").SetParameter("@startTime", startTime).SetParameter("@finishTime", finishTime).SetParameter("@id", ID)
                    .ExecuteNonQuery();

            if (res != 1)
            {
                Logger.Error("error updating the resumed production. " + this);
            }
        }

        private void PauseProduction()
        {
            if (paused)
            {
                Logger.Warning("production already paused: " + this);
                return;
            }

            paused = true;
            pauseTime = DateTime.Now;

            var res=
            Db.Query().CommandText("update runningproduction set paused=1,pausetime=@pauseTime where id=@id").SetParameter("@pauseTime", pauseTime).SetParameter("@id", ID)
                .ExecuteNonQuery();

            if (res != 1)
            {
                Logger.Error("error updating the paused production. " + this);
            }
        }

        public ErrorCodes HasAccess(Character issuerCharacter)
        {
            return character == issuerCharacter ? ErrorCodes.NoError : ErrorCodes.AccessDenied;
        }

        /// <summary>
        /// This command refreshes the corporation production info
        /// Technically corp members with proper roles see their corpmate's production events. start/end/cancel
        /// </summary>
        /// <param name="command"></param>
        private void SendProductionEventToCorporationMembers(Command command)
        {
            if (!useCorporationWallet)
                return;

            var replyDict = new Dictionary<string, object> {{k.production, ToDictionary()}};

            const CorporationRole roleMask = CorporationRole.CEO | CorporationRole.DeputyCEO | CorporationRole.ProductionManager | CorporationRole.Accountant;
            Message.Builder.SetCommand(command)
                .WithData(replyDict)
                .ToCorporation(character.CorporationEid, roleMask)
                .Send();
        }

        public void SendProductionEventToCorporationMembersOnCommitted(Command command)
        {
            Transaction.Current.OnCommited(()=> SendProductionEventToCorporationMembers(command));
        }

        public void InsertProductionInProgess()
        {
            //insert new running production
            var id = Db.Query().CommandText(@"insert runningproduction (characterID,characterEID,resultDefinition,type,startTime,finishTime,facilityEID,totalProductionTime,baseEID,creditTaken,pricePerSecond,amountofcycles,useCorporationWallet) values
								(@characterID,@characterEID,@resultDefinition,@type,@startTime,@finishTime,@facilityEID,@totalProductionTime,@baseEID,@creditTaken,@pricePerSecond,@amountOfCycles,@useCorporationWallet);
								select cast(scope_identity() as int)")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@characterEID", character.Eid)
                .SetParameter("@resultDefinition", resultDefinition)
                .SetParameter("@type", (int) type)
                .SetParameter("@startTime", startTime)
                .SetParameter("@finishTime", finishTime)
                .SetParameter("@facilityEID", facilityEID)
                .SetParameter("@totalProductionTime", totalProductionTimeSeconds)
                .SetParameter("@baseEID", baseEID)
                .SetParameter("@creditTaken", creditTaken)
                .SetParameter("@pricePerSecond", pricePerSecond)
                .SetParameter("@amountOfCycles", amountOfCycles)
                .SetParameter("@useCorporationWallet", useCorporationWallet)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            //save reserved EIDs
            foreach (var eid in ReservedEids)
            {
                Db.Query().CommandText("insert runningproductionreserveditem (runningid, reservedeid) values (@runningID, @reservedEID)")
                    .SetParameter("@runningID", id)
                    .SetParameter("@reservedEID", eid)
                    .ExecuteNonQuery();
            }

            //store ID
            ID = id;
        }
    }
}
