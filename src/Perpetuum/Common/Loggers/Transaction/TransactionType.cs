namespace Perpetuum.Common.Loggers.Transaction
{
    /// <summary>
    /// Types of the transaction logs. Both corporations and characters.
    /// </summary>
    public enum TransactionType
    {
        hangarRent, 
        marketBuy, 
        warDeclaration, 
        characterPayOut, 
        hangarRentAuto, 
        characterDonate, 
        marketFee, 
        marketSell, 
        buyOrderDeposit, 
        buyOrderPayBack, 
        missionPayOut, //10
        alliaceCreate,
        corporationCreate,
        refund,
        extensionLearn,
        ItemRepair,
        corporationTransfer_to,
        corporationTransfer_from,
        ProductionManufacture,
        ProductionLicenseCreate,
        ProductionPatentMaterialEfficiencyDevelop, //20
        ProductionPatentNofRunsDevelop,
        ProductionPatentTimeEfficiencyDevelop,
        characterTransfer_to,
        characterTransfer_from,
        ProductionResearch,
        ProductionMultiItemRepair,
        ProductionPrototype,
        ProductionMassProduction,
        MissionTax, 
        TradeSpent, //30
        TradeGained,
        InsuranceFee, // 32
        InsurancePayOut,
        BoxRequest,
        MarketTax,
        CharacterCreate,
        SiegeFee,
        SiegeFeeRefund,
        SiegeWon,
        BaseIncome,
        SiegePoolPayback,
        SiegeSiteClaim,
        ModifyMarketOrder,

        TradeItemIn,
        TradeItemOut,
        PutLoot,
        TakeLoot, //47
        TrashItem,

        RefineDelete,
        RefineCreated,

        ReprocessDeleted,
        ReprocessCreated,

        PrototypeCreated,
        PrototypeDeleted,

        ResearchCreated,
        ResearchDeleted,

        MassProductionDeleted, //57
        MassProductionCreated, //58

        ItemDonate, //59
        ItemObtain, //60

        PlayerDeath,
        ItemSupply,
        ExtensionPriceRefund,
        SparkUnlock, 
        SparkActivation, //65
        ItemDeploy, //66
        AddContainerContent, //67
        DocumentRent, //68
        DocumentCreate, //69

        ResearchKitMerge, //70

        ProductionCPRGForge, //71
        CPRGForgeDeleted, //72
        ItemShopBuy, //73
        ItemShopTake, //74
        
        GoodiePackCredit, //75

        TransportAssignmentSubmit, //76
        TransportAssignmentCollateral, //77
        TransportAssignmentDeliver, //78
        TransportAssignmentBonus,
        TransportAssignmentCollateralPayback,
        TransportAssignmentRewardPayback,
        TransportAssignmentCollateralPaybackOnGiveUp,
        SparkTeleportUse, // 83
        SparkTeleportPlace, //84

        MissionItemDeliver, //85
        MissionRewardTake, //86

        PBSReimburse, //87
        
        LotteryOpen, //88
        LotteryRandomItemCreated, //89
        ItemRedeem, //90
        ItemShopCreditTake,
        GiftOpen,
        GiftRandomItemCreated,
        
    }
}