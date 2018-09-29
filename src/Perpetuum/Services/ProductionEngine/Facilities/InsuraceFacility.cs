using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Robots;
using Perpetuum.Services.Insurance;

namespace Perpetuum.Services.ProductionEngine.Facilities
{
    public class InsuraceFacility : ProductionFacility
    {
        private readonly InsuranceHelper _insuranceHelper;

        public InsuraceFacility(InsuranceHelper insuranceHelper)
        {
            _insuranceHelper = insuranceHelper;
        }

        public override Dictionary<string, object> GetFacilityInfo(Character character)
        {
            var infoDict = base.GetFacilityInfo(character);

            infoDict.Add(k.maxAmount, RealMaxSlotsPerCharacter(character));

            return infoDict;
        }

        public override ProductionFacilityType FacilityType
        {
            get { return ProductionFacilityType.NotDefined; }
        }

        public override int RealMaxSlotsPerCharacter(Character character)
        {
            return 1 + GetSlotExtensionBonus(character);
        }

        public override int GetSlotExtensionBonus(Character character)
        {
            return (int) character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_INSURANCE_SLOTS);
        }

        public double GetFeeExtensionBonus(Character character)
        {
            var bonus = character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_INSURANCE_FEE);
            return bonus;
        }

        public override double GetTimeExtensionBonus(Character character)
        {
            var bonus = character.GetExtensionsBonusSummary(ExtensionNames.PRODUCTION_INSURANCE_TIME);
            return (int) bonus;
        }

        public ErrorCodes GetInsurancePrice(Item item, out double insuranceFee, out double payOut)
        {
            insuranceFee = 0;
            payOut = 0;
            return InsuranceHelper.GetInsurancePrice(item.Definition, ref insuranceFee, ref payOut);
        }

        public int GetInsuranceDays(Character character)
        {
            var bonusDays = GetTimeExtensionBonus(character);
            return (int)(15 + bonusDays);
        }

        public void InsuranceBuy(Character character, IEnumerable<long> robotEids)
        {
            //auto clean
            InsuranceHelper.CleanUpInsurances();

            var currentInsurances = InsuranceHelper.CountInsurances(character);
            var maxInsurances = RealMaxSlotsPerCharacter(character);
            var insuranceDays = GetInsuranceDays(character);

            Corporation usedCorporation = null;

            foreach (var robot in robotEids.Select(RobotHelper.LoadRobotOrThrow))
            {
                try
                {
                    InsuranceBuy(character, robot, ref currentInsurances, maxInsurances, insuranceDays, ref usedCorporation);
                }
                catch (PerpetuumException gex)
                {
                    character.SendItemErrorMessage(Commands.ProductionInsuranceBuy, gex.error, robot);
                }
            }

            usedCorporation?.SendInsuranceList();
        }

        public void InsuranceBuy(Character character, Robot robot, ref int currentInsurances, int maxInsurances, int insuranceDays, ref Corporation corporation)
        {
            robot.ED.ThrowIfEqual(Robot.NoobBotEntityDefault, ErrorCodes.DefinitionNotSupported);
            robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);

            robot.Initialize(character);
            robot.CheckOwnerCharacterAndCorporationAndThrowIfFailed(character);
            corporation = character.GetCorporation();

            long? corporationEid = character.CorporationEid;
            var useCorporationWallet = false;
            var endDate = DateTime.Now.AddDays(insuranceDays);

            var insurance = _insuranceHelper.GetInsurance(robot.Eid);

            _insuranceHelper.GetConditions(character, robot, insurance, (long) corporationEid, insuranceDays, maxInsurances, currentInsurances, ref endDate, ref useCorporationWallet).ThrowIfError();

            var wallet = character.GetWalletWithAccessCheck(useCorporationWallet, TransactionType.InsuranceFee);

            double insuranceFee, payOut;
            GetInsurancePrice(robot, out insuranceFee, out payOut).ThrowIfError();

            wallet.Balance -= insuranceFee;

            var b = TransactionLogEvent.Builder()
                .SetCharacter(character)
                .SetTransactionType(TransactionType.InsuranceFee)
                .SetCreditBalance(wallet.Balance)
                .SetCreditChange(-insuranceFee)
                .SetItem(robot);

            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corporation.LogTransaction(b);
            }
            else
            {
                character.LogTransaction(b);
            }

            if (!useCorporationWallet)
            {
                corporationEid = null;
            }

            InsuranceHelper.AddInsurance(character, robot.Eid, endDate, InsuranceType.robotInsurance, corporationEid, payOut).ThrowIfError();
            character.GetCurrentDockingBase().AddCentralBank(TransactionType.InsuranceFee, insuranceFee);
            currentInsurances++;
        }

        public ErrorCodes InsuranceQuery(Character character, IEnumerable<long> targetEids, out Dictionary<string, object> result)
        {
            var ec = ErrorCodes.NoError;
            result = new Dictionary<string, object>();

            var corporationEid = character.CorporationEid;
            var insuranceDays = GetInsuranceDays(character);

            var maxInsurances = RealMaxSlotsPerCharacter(character);
            var currentInsurances = InsuranceHelper.CountInsurances(character);

            var counter = 0;
            foreach (var eid in targetEids)
            {
                var useCorporationWallet = false;
                var insurance = _insuranceHelper.GetInsurance(eid);
                var endDate = DateTime.Now.AddDays(insuranceDays);

                var item = Item.GetOrThrow(eid);

                var robot = item as Robot;

                if (robot == null)
                {
                    ec = ErrorCodes.InsuranceAllowedForRobotsOnly;
                    continue;
                }

                if (!robot.IsSingleAndUnpacked)
                {
                    ec = ErrorCodes.RobotMustbeSingleAndNonRepacked;
                    continue;
                }

                double price, payOut;
                if ((ec = GetInsurancePrice(robot, out price, out payOut)) != ErrorCodes.NoError)
                {
                    continue;
                }

                var oneRobot = new Dictionary<string, object>
                {
                    {k.price, price},
                    {k.payOut, payOut},
                    {k.eid, robot.Eid},
                };

                if ((ec = _insuranceHelper.GetConditions(character, robot, insurance, corporationEid, insuranceDays, maxInsurances, currentInsurances, ref endDate, ref useCorporationWallet)) != ErrorCodes.NoError)
                {
                    continue;
                }

                oneRobot.Add(k.endTime, endDate); //ez lesz ha befizet ra

                if (insurance != null)
                {
                    oneRobot.Add(k.current, insurance.endDate); //ez a mostani, mert ez mar biztositva van
                }

                oneRobot.Add(k.useCorporationWallet, useCorporationWallet);

                result.Add("i" + counter ++, (object) oneRobot);
            }

            if (targetEids.Count() == 1)
            {
                return ec;
            }

            return ErrorCodes.NoError;
        }
    }
}