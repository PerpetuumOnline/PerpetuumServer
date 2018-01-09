namespace Perpetuum.Zones.PBS
{
    /// <summary>
    /// Additional information for a pbs node update
    /// </summary>
    public enum PBSEventType
    {
        baseDeadHomeBaseCleared, //0
        baseDeadWhileOnZone, //1
        baseDeadWhileDocked, //2
        nodeUpdate, //3
        nodeDeployed, //4
        baseDead, //5
        baseDeployed, //6
        nodeDead,
        nodeAttacked
    }
}