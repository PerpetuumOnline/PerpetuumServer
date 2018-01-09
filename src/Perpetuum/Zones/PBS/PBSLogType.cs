namespace Perpetuum.Zones.PBS
{
    /// <summary>
    /// pbs node's log type
    /// </summary>
    public enum PBSLogType
    {
        deployed, //0
        constructed, //1
        deconstructed, //2
        killed, //3
        online, //4
        offline, //5
        takeOver, //6
        materialSubmitted, //7
        connected, //8
        disconnected,
        reinforceStart, //10
        reinforceEnd, //11
        gotOrphaned, //12
        gotConnected, //13
        vulnerableStart, //14
        vulnerableEnd, //15
        wellDepleted //16
    }
}