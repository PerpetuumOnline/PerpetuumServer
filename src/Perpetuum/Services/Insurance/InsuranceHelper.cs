using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;

namespace Perpetuum.Services.Insurance
{
    public class InsuranceHelper
    {
        private readonly InsurancePayOut _insurancePayOut;
        public static double InsurancePayOutMultiplier = 0.90;
        public static double InsuranceFeeMultiplier = 1.0;

        public InsuranceHelper(InsurancePayOut insurancePayOut)
        {
            _insurancePayOut = insurancePayOut;
        }

        public bool CheckInsuranceOnDeath(long eid, int definition)
        {
            var insurance = GetInsurance(eid);
            if (insurance == null) 
                return false;

            _insurancePayOut.PayOut(insurance,definition);
            DeleteAndInform(insurance,eid);
            return true;
        }

        public void DeleteAndInform(InsuranceDescription insurance,long eid)
        {
            if (insurance == null)
                return;

            DeleteInsurance(eid);

            if (insurance.corporationEid != null)
            {
                var corporation = Corporation.GetOrThrow((long)insurance.corporationEid);
                corporation.SendInsuranceList();
            }
            else
            {
                SendInsuranceListToCharacter(insurance.character);
            }
        }

        [CanBeNull]
        public InsuranceDescription GetInsurance(long itemEid)
        {
            var record = Db.Query().CommandText("select eid,insurancetype,enddate,characterid,corporationeid,payout from insurance where eid=@EID and enddate > getdate()")
                                 .SetParameter("@EID", itemEid)
                                 .ExecuteSingleRow();

            if (record == null)
                return null;

            var d = new InsuranceDescription();

            // eid,insurancetype,enddate,characterid,corporationeid,payout
            d.eid = record.GetValue<long>(0);
            d.type = (InsuranceType)record.GetValue<int>(1);
            d.endDate = record.GetValue<DateTime>(2);
            d.character = Character.Get(record.GetValue<int>(3));
            d.corporationEid = record.GetValue<long?>(4);
            d.payOutPrice = record.GetValue<double>(5);

            return d;
        }

        public static bool IsInsured(long eid)
        {
            var endDate =
                Db.Query().CommandText("select enddate from insurance where eid=@EID").SetParameter("@EID", eid)
                        .ExecuteScalar<DateTime>();

            if (endDate.Equals(default(DateTime)) || endDate < DateTime.Now)
            {
                return false;
            }

            return true;

        }

        public static bool IsInsured(IEnumerable<long> eids )
        {
            var eidString = eids.ArrayToString();

            var res =
                Db.Query().CommandText("select count(*) from insurances where eid in (" + eidString + ") and enddate < getdate()").ExecuteScalar<int>();

            return (res > 0);
        }

        public static int CleanUpOnCorporationLeave(Character character, long corporationEid)
        {
            return Db.Query().CommandText("delete insurance where (characterId=@characterID and corporationeid=@corporationEID) or (corporationeid=@corporationEID and eid in (select eid from entities where owner=@owner))")
                           .SetParameter("@characterID", character.Id)
                           .SetParameter("@corporationEID", corporationEid)
                           .SetParameter("@owner",character.Eid)
                           .ExecuteNonQuery();
        }

        //returns the list of insuraces a character has
        public static Dictionary<string, object> InsuranceList(Character character)
        {
            var counter = 0;
            return Db.Query().CommandText("select i.eid,i.insurancetype,i.enddate,e.owner,e.parent,e.ename,i.corporationeid,i.payout,e.definition from insurance i join entities e on i.eid=e.eid where (i.characterid=@characterID or i.corporationeid=@corpEID) and enddate > getdate()")
                           .SetParameter("@characterID", character.Id)
                           .SetParameter("@corpEID", character.CorporationEid)
                           .Execute()
                           .Select(r => (object) InsuranceInfoRecordToDictionary(r)).ToDictionary(r => "i" + counter++);
        }

        private static Dictionary<string, object>  InsuranceInfoRecordToDictionary(IDataRecord record)
        {

            return new Dictionary<string, object>
                {
                    {k.eid, record.GetValue<long>(0)},
                    {k.type, record.GetValue<int>(1)},
                    {k.endTime, record.GetValue<DateTime>(2)},
                    {k.owner, record.GetValue<long?>(3)},
                    {k.parent, record.GetValue<long?>(4)},
                    {k.name, record.GetValue<string>(5)},
                    {k.corporationEID, record.GetValue<long?>(6)},
                    {k.payOut, record.GetValue<double>(7)},
                    {k.definition, record.GetValue<int>(8)},
                };
        }


        public static Dictionary<long, DateTime> GetInsuranceEndDates(int characterId)
        {
            return 
                Db.Query().CommandText("select eid,enddate from insurance where characterid=@characterID and enddate > getdate()").SetParameter("@characterID", characterId)
                        .Execute()
                        .ToDictionary(record => record.GetValue<long>(0), record => record.GetValue<DateTime>(1));

        }


        public static ErrorCodes AddInsurance(Character character, long eid, DateTime endDate, InsuranceType insuranceType, long? corporationEid, double payOut)
        {
            object corpobject = DBNull.Value;

            if (corporationEid != null)
            {
                corpobject = (long) corporationEid;
            }

            Db.Query().CommandText("insuranceadd")
                    .SetParameter("@characterID", character.Id)
                    .SetParameter("@EID", eid)
                    .SetParameter("@endDate", endDate)
                    .SetParameter("@type", (int) insuranceType)
                    .SetParameter("@corporationEID", corpobject)
                    .SetParameter("@payOut", payOut)
                    .ExecuteNonQuery();

            return ErrorCodes.NoError;

        }


        public static int CountInsurances(Character character)
        {
            return  Db.Query().CommandText("select count(*) from insurance where characterid=@characterID and enddate > getdate()")
                            .SetParameter("@characterID", character.Id)
                            .ExecuteScalar<int>();

        }

        public static void DeleteInsurance(long eid)
        {
            Db.Query().CommandText("delete insurance where eid=@EID")
                .SetParameter("@EID", eid)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLDeleteError);
        }

        public static bool IsItemInsuredByCharacter(int characterId, long eid)
        {

            var res =
                Db.Query().CommandText("select count(*) from insurance where characterId=@characterID and eid=@EID and enddate > getdate()").SetParameter("@characterID", characterId).SetParameter("@EID", eid)
                        .ExecuteScalar<int>();

            return (res == 1);

        }

        public static void CleanUpInsurances()
        {
            var res =
                Db.Query().CommandText("delete insurance where enddate < getdate()").ExecuteNonQuery();

            if (res > 0)
            {
                Logger.Info("insurance cleanup deleted " + res + " records");
            }
        }


        public static Dictionary<string, object> GetCorporationInsurances(long corporationEid)
        {

            var counter = 0;
            return
                (from r in
                     Db.Query().CommandText("select i.eid,i.insurancetype,i.enddate,e.owner,e.parent,e.ename,i.corporationeid,i.payout,e.definition from insurance i join entities e on i.eid=e.eid where i.corporationeid=@corpEID and enddate > getdate()").SetParameter("@corpEID", corporationEid)
                             .Execute()
                 select (object)InsuranceInfoRecordToDictionary(r)).ToDictionary(r => "i" + counter++);

        }



        public static void SendInsuranceListToCharacter(Character character)
        {
            //client display stuff
            var messageBuilder = Message.Builder.SetCommand(Commands.ProductionInsuranceList).ToCharacter(character);

            var list = InsuranceList(character);
            if (list.Count > 0)
            {
                var maxSlots = character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_INSURANCE_SLOTS);
                messageBuilder.SetData(k.insurance, list).SetData(k.maxAmount, maxSlots);
            }
            else
            {
                messageBuilder.WithEmpty();
            }

            messageBuilder.Send();
        }

        public static void SendEmptyCorporationInsuranceList(Character character)
        {
            Message.Builder.SetCommand(Commands.ProductionCorporationInsuranceList).WithEmpty().ToCharacter(character).Send();
        }

        public void DeleteAndInform(Item item)
        {
            var insurance = GetInsurance(item.Eid);
            DeleteAndInform(insurance,item.Eid);
        }

        public ErrorCodes GetConditions(Character character, Item item, InsuranceDescription insurance, long corporationEid, int insuranceDays, int maxInsurances, int currentInsurances, ref DateTime endDate, ref bool useCorporationWallet )
        {
            var ec = ErrorCodes.NoError;

            if (insurance != null)
            {
                //a robot mar biztositva van

                Logger.Info("robot already insured. " + insurance);

                if (insurance.corporationEid != null)
                {
                    Logger.Info("corp insured robot. ");

                    if (insurance.corporationEid == corporationEid)
                    {
                        Logger.Info("insured by my corp, extend");

                        //extend date
                        endDate = insurance.endDate.AddDays(insuranceDays);
                        useCorporationWallet = true;

                        if (insurance.character != character)
                        {
                            Logger.Info("wasnt isured by me, extension check");

                            if (maxInsurances <= currentInsurances)
                            {
                                return ErrorCodes.MaximumInsurancesExceeded;
                            }


                        }



                    }
                    else
                    {
                        Logger.Info("new insurance for me, check extension");


                        if (maxInsurances <= currentInsurances)
                        {
                            return ErrorCodes.MaximumInsurancesExceeded;
                        }

                        Logger.Info("insured by other corp, delete insurance");

                        DeleteAndInform(insurance,item.Eid);

                        if (item.Owner == corporationEid)
                        {
                            Logger.Info("belongs to corp, use corpwallet");

                            useCorporationWallet = true;
                        }
                    }



                }
                else
                {
                    Logger.Info("privately insured robot");

                    if (insurance.character == character)
                    {
                        Logger.Info("i&i insured it.");


                        if (item.Owner == corporationEid)
                        {
                            Logger.Info("owner is my corp, private insurance in corp storage");

                            useCorporationWallet = true;
                            endDate = insurance.endDate.AddDays(insuranceDays);


                        }
                        else
                        {
                            Logger.Info("owner is me, extend date");

                            endDate = insurance.endDate.AddDays(insuranceDays);


                        }


                    }
                    else
                    {
                        Logger.Info("someone else insured it.");

                        DeleteAndInform(insurance,item.Eid);

                        if (item.Owner == corporationEid)
                        {
                            useCorporationWallet = true;
                            endDate = insurance.endDate.AddDays(insuranceDays);
                        }



                        if (maxInsurances <= currentInsurances)
                        {
                            return ErrorCodes.MaximumInsurancesExceeded;
                        }


                    }



                }


            }
            else
            {

                if (item.Owner == corporationEid)
                {
                    useCorporationWallet = true;
                }


                //this will be a new insurance, so check the extension amount
                if (maxInsurances <= currentInsurances)
                {
                    return ErrorCodes.MaximumInsurancesExceeded;
                }

            }


            return ec;

        }

        public static bool IsPrivateInsured(long eid)
        {
            return Db.Query().CommandText("select count(*) from insurance where eid=@EID and corporationeid is NULL").SetParameter("@EID", eid)
                           .ExecuteScalar<int>() >= 1;
        }
        private static readonly ConcurrentDictionary<int, InsurancePrice> _insurancePrices = new ConcurrentDictionary<int, InsurancePrice>();


        public static ErrorCodes GetInsurancePrice(int definition, ref double insuranceFee, ref double insurancePayOut)
        {
            var ec = ErrorCodes.NoError;
            insuranceFee = 0;
            insurancePayOut = 0;

            InsurancePrice insurancePrice;
            if (_insurancePrices.TryGetValue(definition, out insurancePrice))
            {

                if (insurancePrice == null)
                {
                    return ErrorCodes.WTFErrorMedicalAttentionSuggested;
                }


            }
            else
            {
                var record = Db.Query().CommandText("select fee,payout from insuranceprices where definition=@definition").SetParameter("@definition", definition)
                                     .ExecuteSingleRow();

                if (record == null)
                {
                    _insurancePrices.AddOrUpdate(definition, o => null, (k, v) => null);
                    Logger.Info("no record was found for definition: " + definition + " " + EntityDefault.Get(definition).Name);
                    return ErrorCodes.WTFErrorMedicalAttentionSuggested;
                }

                insurancePrice = new InsurancePrice
                    {
                        definition = definition,
                        fee = record.GetValue<double>(0),
                        payOut = record.GetValue<double>(1)
                    };


                _insurancePrices.AddOrUpdate(definition, insurancePrice, (k, v) => v);
            }

            insuranceFee = insurancePrice.fee;
            insurancePayOut = insurancePrice.payOut;
            return ec;

        }

        public static ErrorCodes LoadInsurancePrices()
        {
            foreach (var iPrice in
                Db.Query().CommandText("select definition,fee,payout from insuranceprices").Execute()
                        .Select(r => new InsurancePrice
                            {
                                definition = r.GetValue<int>(0),
                                fee = r.GetValue<double>(1),
                                payOut = r.GetValue<double>(2)
                            }))
            {
                var price = iPrice;
                _insurancePrices.AddOrUpdate(iPrice.definition, iPrice, (k, v) => price);
            }

            return ErrorCodes.NoError;

        }

        public static Dictionary<string, object> GetInsuranceState()
        {
            var counter = 0;
            return _insurancePrices.Values
                                   .Select(i => (object) i.ToDictionary())
                                   .ToDictionary(d => "i" + counter++);

        }

        public static void RemoveAll(Character character)
        {
            Db.Query().CommandText("delete insurance where characterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .ExecuteNonQuery();
        }

    }
}