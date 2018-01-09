namespace Perpetuum.Accounting
{
    public enum AccountTransactionType
    {
        Undefined, //0
        Purchase, //1 ezt akkor kapja amikor vesz creditet penzert
        ExistingSubscription, //2 ezt a kezdeti konvertalaskor kapja
        CorporationRename, //3 
        CharacterRename, //4
        ExtensionReset, //5
        ExtensionRemoveLevel,//6
        EpBoost,//7
        FromIce, //8 konvertalas stuff
        FreeLockedEp, //9
        IceRedeem, //10
        EPRedeem, //11
        CreditRedeem, //12
        SparkRedeem //13
    }
}